namespace UglyToad.PdfPig.Tests.Writer
{
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    public class NoTextTokenWriterTests
    {
        private const string ContentWithText = "1 0 0 1 5 5 cm\nBT\n/F1 12 Tf\n10 10 Td\n(Hello World) Tj\nET\n10 10 m 100 100 l S\n";

        private const string ContentWithoutText = "1 0 0 1 5 5 cm\n10 10 m 100 100 l S\n";

        [Fact]
        public void UsesSuppliedFilterProviderToDecodeContentStream()
        {
            var provider = CustomNameFilterProvider.Create();
            var streamToken = CreateContentStream(CustomNameFilterProvider.CustomFlateName, ContentWithText);

            var written = Write(streamToken, provider);

            Assert.Equal(1, provider.CustomFilterDecodeCount);

            // The stream was rewritten, so it is re-encoded with the standard filter.
            var decoded = DecodeWrittenStream(written, NameToken.FlateDecode.Data);

            Assert.DoesNotContain("Hello World", decoded);
            Assert.DoesNotContain("Tj", decoded);
            Assert.Contains("cm", decoded);
            Assert.Contains("S", decoded);
        }

        [Fact]
        public void WithoutTheSuppliedFilterProviderTheStreamCannotBeDecoded()
        {
            // Guards the assertion above: the default provider knows nothing about the custom filter name so it
            // fails to decode, and NoTextTokenWriter copies the stream through unchanged.
            var streamToken = CreateContentStream(CustomNameFilterProvider.CustomFlateName, ContentWithText);

            var written = Write(streamToken, DefaultFilterProvider.Instance);

            var decoded = DecodeWrittenStream(written, CustomNameFilterProvider.CustomFlateName);

            Assert.Contains("Hello World", decoded);
        }

        [Fact]
        public void StreamWithoutTextIsWrittenUnchanged()
        {
            var provider = CustomNameFilterProvider.Create();
            var streamToken = CreateContentStream(CustomNameFilterProvider.CustomFlateName, ContentWithoutText);

            var written = Write(streamToken, provider);

            Assert.Equal(1, provider.CustomFilterDecodeCount);

            // No text operation was found so the original (custom filtered) stream is preserved.
            var decoded = DecodeWrittenStream(written, CustomNameFilterProvider.CustomFlateName);

            Assert.Contains("cm", decoded);
        }

        [Fact]
        public void NonPageContentStreamIsNotDecoded()
        {
            var provider = CustomNameFilterProvider.Create();
            var streamToken = CreateContentStream(CustomNameFilterProvider.CustomFlateName, ContentWithText);

            var tokenWriter = new NoTextTokenWriter(provider) { WritingPageContents = false };

            using (var output = new MemoryStream())
            {
                tokenWriter.WriteToken(streamToken, output);
            }

            Assert.Equal(0, provider.CustomFilterDecodeCount);
        }

        [Fact]
        public void AcceptsBothLookupAndPlainFilterProviders()
        {
            var provider = CustomNameFilterProvider.Create();
            var streamToken = CreateContentStream(CustomNameFilterProvider.CustomFlateName, ContentWithText);

            var plain = DecodeWrittenStream(Write(streamToken, provider), NameToken.FlateDecode.Data);
            var lookup = DecodeWrittenStream(Write(streamToken, new FilterProviderWithLookup(provider)), NameToken.FlateDecode.Data);

            Assert.Equal(plain, lookup);
            Assert.DoesNotContain("Hello World", plain);
        }

        private static byte[] Write(StreamToken streamToken, IFilterProvider filterProvider)
        {
            var tokenWriter = new NoTextTokenWriter(filterProvider) { WritingPageContents = true };

            using (var output = new MemoryStream())
            {
                tokenWriter.WriteToken(streamToken, output);
                return output.ToArray();
            }
        }

        private static StreamToken CreateContentStream(string filterName, string content)
        {
            var compressed = DataCompressor.CompressBytes(OtherEncodings.StringAsLatin1Bytes(content));

            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Length, new NumericToken(compressed.Length) },
                { NameToken.Filter, NameToken.Create(filterName) }
            });

            return new StreamToken(dictionary, compressed);
        }

        /// <summary>
        /// Pulls the stream data back out of the bytes emitted by <see cref="NoTextTokenWriter"/> and inflates it,
        /// asserting the written dictionary declares <paramref name="expectedFilterName"/> as its filter.
        /// </summary>
        private static string DecodeWrittenStream(byte[] written, string expectedFilterName)
        {
            var text = OtherEncodings.BytesAsLatin1String(written);

            Assert.Contains($"/Filter /{expectedFilterName}", text);

            var start = text.IndexOf("stream", StringComparison.Ordinal) + "stream".Length;
            while (written[start] == '\r' || written[start] == '\n')
            {
                start++;
            }

            var end = text.LastIndexOf("endstream", StringComparison.Ordinal);
            while (end > start && (written[end - 1] == '\r' || written[end - 1] == '\n'))
            {
                end--;
            }

            var data = new byte[end - start];
            Array.Copy(written, start, data, 0, data.Length);

            var decoded = new FlateFilter().Decode(
                data,
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                DefaultFilterProvider.Instance,
                0);

            return OtherEncodings.BytesAsLatin1String(decoded.Span);
        }
    }
}
