namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class FlateFilterTests
    {
        private readonly FlateFilter filter = new FlateFilter();

        [Fact]
        public void EncodeAndDecodePreservesInput()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = new byte[] { 67, 69, 69, 10, 4, 20, 6, 19, 120, 64, 64, 64, 32 };

            using (var inputStream = new MemoryStream(input))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                var result = filter.Encode(inputStream, parameters);
                var decoded = filter.Decode(result, parameters, TestFilterProvider.Instance, 0);
                Assert.Equal(input, decoded.ToArray());
            }
        }

        [Fact]
        public void CanDecodeCorruptedInputIssue1235()
        {
            const string hexStr =
                "789C958D5D0AC2400C844FB077980B74BB7FD9D982F820B43E8B7B03C542C187EAFDC1F84B7D1164200999E49BD9044C6653D10E1E443DA1AF6636ED76EF315E7572968E1ECDAB7FB7506C4C59C0AEB3912EE270366AAAF4E36D364BF7911450DC274A5112B1AC9751D77A58680B51A4D8AE433D62953C037396E0F290FBE098B267A43051725AA34E77E44EF50B1B52B42C90E4ADF83FB94FDD0000000000";

            var hex = new HexToken(hexStr.AsSpan());

            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var result = filter.Decode(hex.Bytes.ToArray(), parameters, TestFilterProvider.Instance, 0);

            var text = OtherEncodings.BytesAsLatin1String(result.ToArray());

            Assert.StartsWith("q", text);
        }

        [Fact]
        public void DataThatIsNoDeflateStreamDoesNotComeBackAsThoughItWereDecoded()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = OtherEncodings.StringAsLatin1Bytes("This is not a deflate stream.");

            var decoded = filter.Decode(input, parameters, TestFilterProvider.Instance, 0);

            // The input used to be handed straight back, which a caller cannot tell apart
            // from content that happens to look like that.
            Assert.NotEqual(input, decoded.ToArray());
        }

        [Fact]
        public void TruncatedStreamKeepsWhatInflated()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            // Comfortably more than one deflate block: the inflater of .NET Framework
            // hands over completed blocks only, so a stream small enough to be a single
            // block yields nothing there while .NET yields a prefix of it.
            var content = new StringBuilder();
            for (var i = 0; content.Length < 200000; i++)
            {
                content.Append("Line ").Append(i).Append(" of a document spanning several blocks.\n");
            }

            var original = OtherEncodings.StringAsLatin1Bytes(content.ToString());

            byte[] compressed;
            using (var inputStream = new MemoryStream(original))
            {
                compressed = filter.Encode(inputStream, parameters);
            }

            var half = new byte[compressed.Length / 2];
            Array.Copy(compressed, half, half.Length);

            var decoded = filter.Decode(half, parameters, TestFilterProvider.Instance, 0).ToArray();

            Assert.NotEmpty(decoded);
            Assert.True(decoded.Length < original.Length, "a truncated stream cannot yield everything");
            Assert.Equal(original.AsSpan(0, decoded.Length).ToArray(), decoded);
        }

        [Fact]
        public void AFailureYieldsNothingRatherThanTheInput()
        {
            // A predictor row too wide for any buffer to hold. Nothing here decodes, and the
            // input must not come back dressed as content.
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                // The resolver only hands the parameters over when a filter is named.
                { NameToken.Filter, NameToken.FlateDecode },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Predictor, new NumericToken(12) },
                        { NameToken.Colors, new NumericToken(32) },
                        { NameToken.BitsPerComponent, new NumericToken(16) },
                        { NameToken.Columns, new NumericToken(int.MaxValue) },
                    })
                },
            });

            byte[] compressed;
            using (var inputStream = new MemoryStream(OtherEncodings.StringAsLatin1Bytes("content")))
            {
                compressed = filter.Encode(inputStream, new DictionaryToken(new Dictionary<NameToken, IToken>()));
            }

            var decoded = filter.Decode(compressed, parameters, TestFilterProvider.Instance, 0);

            Assert.Empty(decoded.ToArray());
        }
    }
}
