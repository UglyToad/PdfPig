namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Collections.Generic;
    using System.IO;
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using Xunit;

    public class FlateFilterTests
    {
        private readonly FlateFilter filter = new FlateFilter(new DecodeParameterResolver(new TestingLog()), new PngPredictor(), new TestingLog());

        [Fact]
        public void EncodeAndDecodePreservesInput()
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var input = new byte[] {67, 69, 69, 10, 4, 20, 6, 19, 120, 64, 64, 64, 32};

            using (var inputStream = new MemoryStream(input))
            {
                inputStream.Seek(0, SeekOrigin.Begin);
                var result = filter.Encode(inputStream, parameters, 0);
                var decoded = filter.Decode(result, parameters, 0);
                Assert.Equal(input, decoded);
            }
        }
    }
}
