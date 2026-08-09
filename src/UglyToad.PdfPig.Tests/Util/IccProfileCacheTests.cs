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

    public class IccProfileCacheTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();

        /// <summary>
        /// Counts how often the service is asked to parse - the cost the cache exists to bound - and hands
        /// back a distinct profile instance each time so callers can tell a cached answer from a fresh one.
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

        private sealed class ThrowingProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
                => throw new InvalidOperationException("Simulated parser failure.");
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
        public void DecodesAndParsesTheSameIndirectProfileOnlyOnce()
        {
            // The guarantee PDFBox gets from caching the constructed PDICCBased against the profile stream's
            // COSObject. Without it, TryGetProfile ran once per resource dictionary - so once per page, and
            // again per Form XObject - because ResourceStore clears its colour space caches each time one is
            // loaded.
            var cache = new IccProfileCache();
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
        public void DecodesAndParsesTheSameDirectlyWrittenProfileOnlyOnce()
        {
            // Two separate stream objects with identical content: without an indirect reference to key on
            // the cache has to recognise them by their bytes. This is the shape every ICC image XObject
            // arrives in, because resolving the image dictionary rebuilds the colour space array.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var first = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);
            var second = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);

            Assert.Same(first, second);
            Assert.Equal(1, service.ParseCount);
            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void KeepsDirectlyWrittenProfilesOfTheSameLengthApart()
        {
            // The content key is a hash, so profiles that differ only in their bytes - not their length -
            // are the case that would break if the hash were not part of the key.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var first = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);
            var second = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 6), filters, Scanner, service);

            Assert.NotSame(first, second);
            Assert.Equal(2, filters.DecodeCount);
            Assert.Equal(2, service.ParseCount);
        }

        [Fact]
        public void KeepsDirectlyWrittenProfilesOfDifferentLengthsApart()
        {
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var first = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner, service);
            var second = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4), filters, Scanner, service);

            Assert.NotSame(first, second);
            Assert.Equal(2, filters.DecodeCount);
        }

        [Fact]
        public void DoesNotRetryAFailedIndirectDecode()
        {
            // Retrying a corrupt multi-megabyte stream once per page is exactly the cost the cache exists
            // to avoid, so a failure is cached as emphatically as a success.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var service = new CountingProfileService();
            var reference = Reference(7);
            var stream = ProfileStream(1, 2, 3, 4, 5);

            Assert.Null(cache.GetOrParse(reference, stream, filters, Scanner, service));
            Assert.Null(cache.GetOrParse(reference, stream, filters, Scanner, service));

            Assert.Equal(1, filters.DecodeCount);
            Assert.Equal(0, service.ParseCount); // a stream that would not decode is never handed over
        }

        [Fact]
        public void DoesNotRetryAFailedDirectDecode()
        {
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider(throwOnDecode: true);
            var service = new CountingProfileService();

            Assert.Null(cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner, service));
            Assert.Null(cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3), filters, Scanner, service));

            Assert.Equal(1, filters.DecodeCount);
        }

        [Fact]
        public void DoesNotRetryAProfileTheServiceDeclined()
        {
            // "Parsed, and the answer was no profile" is as worth remembering as a success.
            var cache = new IccProfileCache();
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
            // An embedded CMYK profile is routinely megabytes and nothing is going to read it.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();

            Assert.Null(cache.GetOrParse(Reference(3), ProfileStream(1, 2, 3), filters, Scanner, null));

            Assert.Equal(0, filters.DecodeCount);
        }

        [Fact]
        public void AThrowingServiceCostsTheProfileAndNothingMore()
        {
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();

            Assert.Null(cache.GetOrParse(Reference(4), ProfileStream(1, 2, 3), filters, Scanner,
                new ThrowingProfileService()));
        }

        [Fact]
        public void KeepsCachingContentKeyedProfilesAsTheirNumberGrows()
        {
            // The content-keyed cache is what every ICC image XObject lands in, so a document with many
            // distinct profiles has to keep hitting it rather than fall back to parsing on every lookup.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var firstPass = new IIccProfile?[64];
            for (byte i = 0; i < 64; i++)
            {
                firstPass[i] = cache.GetOrParse(NullToken.Instance, ProfileStream(0, 0, 0, i), filters, Scanner, service);
            }

            Assert.Equal(64, service.ParseCount);

            for (byte i = 0; i < 64; i++)
            {
                Assert.Same(firstPass[i],
                    cache.GetOrParse(NullToken.Instance, ProfileStream(0, 0, 0, i), filters, Scanner, service));
            }

            Assert.Equal(64, service.ParseCount);
            Assert.Equal(64, filters.DecodeCount);
        }

        // Note on what is deliberately NOT tested here: that the decoded bytes are released after parsing.
        // That is structural rather than behavioural - DecodeAndParse holds them in a local and the
        // dictionaries store IIccProfile?, so there is no field left that could retain them - and the only
        // way to assert it would be a GC-and-weak-reference test, which buys a guarantee the type system
        // already gives in exchange for the flakiest kind of test there is.

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
