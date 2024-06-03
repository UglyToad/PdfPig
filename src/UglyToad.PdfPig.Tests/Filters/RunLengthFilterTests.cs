namespace UglyToad.PdfPig.Tests.Filters
{
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class RunLengthFilterTests
    {
        private readonly RunLengthFilter filter = new RunLengthFilter();

        [Fact]
        public void CanDecodeRunLengthEncodedData()
        {
            var data = new byte[]
            {
                // Write the following 6 bytes literally
                5, 0, 1, 2, 69, 12, 9,
                // Repeat 52 (257 - 254) 3 times
                254, 52,
                // Write the following 3 bytes literally
                2, 60, 61, 16,
                // Repeat 12 (257 - 250) 7 times
                250, 12,
                // Write the following 2 bytes literally
                1, 10, 19
            };

            var decoded = filter.Decode(data, new DictionaryToken(new Dictionary<NameToken, IToken>()), 1);
            
            var expectedResult = new byte[]
            {
                0, 1, 2, 69, 12, 9,
                52, 52, 52,
                60, 61, 16,
                12, 12, 12, 12, 12, 12, 12,
                10, 19
            };

            Assert.Equal(expectedResult, decoded.ToArray());
        }

        [Fact]
        public void StopsAtEndOfDataByte()
        {
            var data = new byte[]
            {
                // Repeat 7 (257 - 254) 3 times
                254, 7,
                // Write the following 2 bytes literally
                1, 128, 50,
                // End of Data Byte
                128,
                // Ignore these
                90, 6, 7
            };

            var decoded = filter.Decode(data, new DictionaryToken(new Dictionary<NameToken, IToken>()), 0);

            var expectedResult = new byte[]
            {
                7, 7, 7,
                128, 50
            };

            Assert.Equal(expectedResult, decoded.ToArray());
        }
    }
}
