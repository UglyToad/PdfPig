namespace UglyToad.PdfPig.Tests.Filters
{
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class FlateFilterTests
    {
        private readonly FlateFilter filter = new FlateFilter();

        [Fact]
        public void EncodeAndDecodePreservesInput()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = new byte[] {67, 69, 69, 10, 4, 20, 6, 19, 120, 64, 64, 64, 32};

            using (var inputStream = new MemoryStream(input))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                var result = filter.Encode(inputStream, parameters, 0);
                var decoded = filter.Decode(result, parameters, TestFilterProvider.Instance, 0);
                Assert.Equal(input, decoded.ToArray());
            }
        }
    }
}
