namespace UglyToad.PdfPig.Tests.Filters
{
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
    }
}
