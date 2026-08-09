namespace UglyToad.PdfPig.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Tokens;

    public class IccProfileByteCacheTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();

        [Fact]
        public void DecodesTheSameIndirectProfileOnlyOnce()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();
            var reference = Reference(7);
            var stream = ProfileStream(1, 2, 3, 4, 5);

            var first = cache.GetOrDecode(reference, stream, filters, Scanner);
            var second = cache.GetOrDecode(reference, stream, filters, Scanner);

            Assert.Equal(1, filters.DecodeCount);
            Assert.True(first.Span.SequenceEqual(second.Span));
        }

        [Fact]
        public void DecodesTheSameDirectlyWrittenProfileOnlyOnce()
        {
            // Two separate stream objects with identical content: without an indirect reference to key on
            // the cache has to recognise them by their bytes.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            var first = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner);
            var second = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner);

            Assert.Equal(1, filters.DecodeCount);
            Assert.True(first.Span.SequenceEqual(second.Span));
        }

        [Fact]
        public void KeepsDirectlyWrittenProfilesOfTheSameLengthApart()
        {
            // The content key is a hash, so profiles that differ only in their bytes - not their length -
            // are the case that would break if the hash were not part of the key.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            var first = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner);
            var second = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3, 4, 6), filters, Scanner);

            Assert.Equal(2, filters.DecodeCount);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, first.ToArray());
            Assert.Equal(new byte[] { 1, 2, 3, 4, 6 }, second.ToArray());
        }

        [Fact]
        public void KeepsDirectlyWrittenProfilesOfDifferentLengthsApart()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            var first = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner);
            var second = cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3, 4), filters, Scanner);

            Assert.Equal(2, filters.DecodeCount);
            Assert.Equal(new byte[] { 1, 2, 3 }, first.ToArray());
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, second.ToArray());
        }

        [Fact]
        public void DoesNotRetryAFailedIndirectDecode()
        {
            // Retrying a corrupt multi-megabyte stream once per page is exactly the cost the cache exists
            // to avoid, so a failure is cached as emphatically as a success.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var reference = Reference(7);
            var stream = ProfileStream(1, 2, 3, 4, 5);

            Assert.True(cache.GetOrDecode(reference, stream, filters, Scanner).IsEmpty);
            Assert.True(cache.GetOrDecode(reference, stream, filters, Scanner).IsEmpty);

            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void DoesNotRetryAFailedDirectDecode()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider(throwOnDecode: true);

            Assert.True(cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner).IsEmpty);
            Assert.True(cache.GetOrDecode(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner).IsEmpty);

            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void KeepsCachingContentKeyedProfilesAsTheirNumberGrows()
        {
            // The content-keyed cache is what every ICC image XObject lands in, so a document with many
            // distinct profiles has to keep hitting it rather than fall back to decoding on every lookup.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            for (byte i = 0; i < 64; i++)
            {
                cache.GetOrDecode(NullToken.Instance, ProfileStream(0, 0, 0, i), filters, Scanner);
            }

            Assert.Equal(64, filters.DecodeCount);

            for (byte i = 0; i < 64; i++)
            {
                Assert.Equal(new byte[] { 0, 0, 0, i },
                    cache.GetOrDecode(NullToken.Instance, ProfileStream(0, 0, 0, i), filters, Scanner).ToArray());
            }

            Assert.Equal(64, filters.DecodeCount);
        }

        /// <summary>
        /// Counts how often the service is asked to parse, which is the cost B8 exists to bound.
        /// </summary>
        private sealed class CountingProfileService : IIccProfileService
        {
            private readonly bool succeed;

            public CountingProfileService(bool succeed = true) => this.succeed = succeed;

            public int ParseCount { get; private set; }

            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                ParseCount++;
                profile = succeed ? new StubProfile() : null;
                return profile is not null;
            }
        }

        private sealed class StubProfile : IIccProfile
        {
            public int NumberOfComponents => 3;

            public IReadOnlyList<double> ComponentRanges { get; } = [0, 1, 0, 1, 0, 1];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }

        [Fact]
        public void ParsesTheSameIndirectProfileOnlyOnce()
        {
            // The guarantee PDFBox gets from caching the constructed PDICCBased against the profile stream's
            // COSObject. Caching the bytes alone still left TryGetProfile called once per resource
            // dictionary, because ResourceStore clears its colour space caches each time one is loaded.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();
            var reference = Reference(7);
            var stream = ProfileStream(1, 2, 3, 4, 5);

            var first = cache.GetOrParse(reference, stream, filters, Scanner, service);
            var second = cache.GetOrParse(reference, stream, filters, Scanner, service);

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal(1, service.ParseCount);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void ParsesTheSameDirectlyWrittenProfileOnlyOnce()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var first = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner, service);
            var second = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner, service);

            Assert.Same(first, second);
            Assert.Equal(1, service.ParseCount);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void DoesNotRetryAProfileTheServiceDeclined()
        {
            // "Parsed, and the answer was no profile" is as worth remembering as a success.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService(succeed: false);
            var reference = Reference(9);
            var stream = ProfileStream(1, 2, 3);

            Assert.Null(cache.GetOrParse(reference, stream, filters, Scanner, service));
            Assert.Null(cache.GetOrParse(reference, stream, filters, Scanner, service));

            Assert.Equal(1, service.ParseCount);
        }

        [Fact]
        public void WithoutAServiceNothingIsDecodedOrParsed()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            Assert.Null(cache.GetOrParse(Reference(3), ProfileStream(1, 2, 3), filters, Scanner, null));

            Assert.Equal(0, filters.DecodeCount);
        }

        [Fact]
        public void AThrowingServiceCostsTheProfileAndNothingMore()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();

            Assert.Null(cache.GetOrParse(Reference(4), ProfileStream(1, 2, 3), filters, Scanner,
                new ThrowingProfileService()));
        }

        private sealed class ThrowingProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
                => throw new InvalidOperationException("Simulated parser failure.");
        }

        [Fact]
        public void AFailedDecodeIsNotHandedToTheService()
        {
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var service = new CountingProfileService();

            Assert.Null(cache.GetOrParse(Reference(5), ProfileStream(1, 2, 3), filters, Scanner, service));

            Assert.Equal(0, service.ParseCount);
        }

        [Fact]
        public void TheProfileAndItsBytesShareOneCacheEntry()
        {
            // Both are keyed the same way, so asking for one after the other must not decode twice - which
            // for a content-keyed profile also means hashing it only once per lookup.
            var cache = new IccProfileByteCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();
            var stream = ProfileStream(1, 2, 3, 4, 5);

            var bytes = cache.GetOrDecode(NullToken.Instance, stream, filters, Scanner);
            var profile = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);

            Assert.False(bytes.IsEmpty);
            Assert.NotNull(profile);
            Assert.Equal(1, filters.DecodeCount);
        }

        private static IndirectReferenceToken Reference(long objectNumber)
        {
            return new IndirectReferenceToken(new IndirectReference(objectNumber, 0));
        }

        private static StreamToken ProfileStream(params byte[] data)
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Length, new NumericToken(data.Length) }
            });

            return new StreamToken(dictionary, data);
        }
    }
}
