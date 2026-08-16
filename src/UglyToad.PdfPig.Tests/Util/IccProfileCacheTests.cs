namespace UglyToad.PdfPig.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using PdfPig.Core;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Tokens;

    public class IccProfileCacheTests
    {
        private static readonly TestPdfTokenScanner Scanner = new();

        /// <summary>
        /// Counts how often the service is asked to parse (the cost the cache exists to bound) and hands
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
                profile = succeed ? new TestIccProfile() : null;
                return profile is not null;
            }
            public bool UseOutputIntent => false;

            public string? PreferredOutputIntentSubtype => null;
        }

        private sealed class ThrowingProfileService : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
                => throw new InvalidOperationException("Simulated parser failure.");
            public bool UseOutputIntent => false;

            public string? PreferredOutputIntentSubtype => null;
        }

        [Fact]
        public void DecodesAndParsesTheSameIndirectProfileOnlyOnce()
        {
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
        public void AProfileWithoutAReferenceIsParsedButNotCached()
        {
            // A stream shall be an indirect object (7.3.8) and nothing on the way to the cache resolves the
            // reference away, so this is the malformed-file case: the profile is still usable, there is just
            // nothing to recognise it by next time.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var first = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);
            var second = cache.GetOrParse(NullToken.Instance, ProfileStream(1, 2, 3, 4, 5), filters, Scanner, service);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
            Assert.Equal(2, service.ParseCount);
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
        public void KeepsCachingProfilesAsTheirNumberGrows()
        {
            // A document may embed many distinct profiles (one per image is common in a print-ready file)
            // and every one of them has to keep hitting the cache rather than fall back to parsing.
            var cache = new IccProfileCache();
            var filters = new CountingFilterProvider();
            var service = new CountingProfileService();

            var firstPass = new IIccProfile?[64];
            for (byte i = 0; i < 64; i++)
            {
                firstPass[i] = cache.GetOrParse(Reference(i), ProfileStream(0, 0, 0, i), filters, Scanner, service);
            }

            Assert.Equal(64, service.ParseCount);

            for (byte i = 0; i < 64; i++)
            {
                Assert.Same(firstPass[i],
                    cache.GetOrParse(Reference(i), ProfileStream(0, 0, 0, i), filters, Scanner, service));
            }

            Assert.Equal(64, service.ParseCount);
            Assert.Equal(64, filters.DecodeCount);
        }

        // TODO - Is the below still true?
        // Note on what is deliberately NOT tested here: that the decoded bytes are released after parsing.
        // That is structural rather than behavioural - DecodeAndParse holds them in a local and the
        // dictionaries store IIccProfile?, so there is no field left that could retain them - and the only
        // way to assert it would be a GC-and-weak-reference test, which buys a guarantee the type system
        // already gives in exchange for the flakiest kind of test there is. The dictionary stores
        // IIccProfile?, so there is no field left that could retain the bytes.

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
