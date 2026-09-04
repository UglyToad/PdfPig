namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using PdfPig.Filters;

    public class PngPredictorParameterTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 8)]
        [InlineData(3, 8)]
        [InlineData(4, 16)]
        [InlineData(32, 16)]
        [InlineData(2, 2)]
        [InlineData(1, 4)]
        public void TheParametersTheSpecificationAllowsPass(int colors, int bitsPerComponent)
        {
            var rowLength = PngPredictor.CalculateRowLength(colors, bitsPerComponent, 100);

            Assert.Equal(((100L * colors * bitsPerComponent) + 7) / 8, rowLength);
        }

        [Theory]
        [InlineData(0, 8)]
        [InlineData(-1, 8)]
        [InlineData(33, 8)]
        [InlineData(3, 0)]
        [InlineData(3, 3)]
        [InlineData(3, 7)]
        [InlineData(3, 32)]
        [InlineData(3, -8)]
        [InlineData(32, 1073741824)]
        public void ColoursAndBitsOutsideTheSpecificationAreRefused(int colors, int bitsPerComponent)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(colors, bitsPerComponent, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PngPredictor.Decoder(12, colors, bitsPerComponent, 100));
        }

        [Fact]
        public void TheProductThatUsedToWrapToZeroIsRefused()
        {
            // 536870912 * 32 * 1073741824 is exactly 2^64, which wrapped to a row of no bytes and
            // decoded silently to nothing; the bit count alone now refuses it.
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(32, 1073741824, 536870912));
        }

        [Fact]
        public void AWidthBeyondAnArrayIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(32, 16, int.MaxValue));
        }

        [Fact]
        public void AnInvalidStreamThroughTheFlateFilterYieldsNothing()
        {
            // The filter turns the refusal into an empty result, as it does for damaged data.
            var filter = new FlateFilter();
            var dictionary = new PdfPig.Tokens.DictionaryToken(new System.Collections.Generic.Dictionary<PdfPig.Tokens.NameToken, PdfPig.Tokens.IToken>
            {
                [PdfPig.Tokens.NameToken.Filter] = PdfPig.Tokens.NameToken.FlateDecode,
                [PdfPig.Tokens.NameToken.DecodeParms] = new PdfPig.Tokens.DictionaryToken(new System.Collections.Generic.Dictionary<PdfPig.Tokens.NameToken, PdfPig.Tokens.IToken>
                {
                    [PdfPig.Tokens.NameToken.Predictor] = new PdfPig.Tokens.NumericToken(12),
                    [PdfPig.Tokens.NameToken.Colors] = new PdfPig.Tokens.NumericToken(32),
                    [PdfPig.Tokens.NameToken.BitsPerComponent] = new PdfPig.Tokens.NumericToken(1073741824),
                    [PdfPig.Tokens.NameToken.Columns] = new PdfPig.Tokens.NumericToken(536870912),
                })
            });

            // A valid zlib stream of a few bytes: header, one stored block "abc", checksum.
            byte[] compressed = [0x78, 0x9C, 0x01, 0x03, 0x00, 0xFC, 0xFF, 0x61, 0x62, 0x63, 0x02, 0x4D, 0x01, 0x27];

            var decoded = filter.Decode(compressed, dictionary, DefaultFilterProvider.Instance, 0);

            Assert.True(decoded.IsEmpty);
        }
    }
}
