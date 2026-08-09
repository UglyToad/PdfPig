namespace UglyToad.PdfPig.Tests.Util
{
    using PdfPig.Core;
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
