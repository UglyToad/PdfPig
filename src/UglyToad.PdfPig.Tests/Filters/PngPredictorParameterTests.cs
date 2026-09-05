namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using static FilterTestHelpers;

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
        public void MovingTheInputIsRefusedForADecoderThatDecodesInPlace()
        {
            // Rows decoded where they lie need the row above to stay in the buffer.
            var inPlace = new PngPredictor.Decoder(12, 1, 8, 4, compact: false);
            Assert.Throws<InvalidOperationException>(() => inPlace.RestartInput());

            var separate = new PngPredictor.Decoder(12, 1, 8, 4);
            separate.RestartInput();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ARowOfNoSamplesIsRefused(int columns)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(1, 8, columns));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PngPredictor.Decoder(2, 1, 8, columns));
        }

        [Fact]
        public void TheWidestRowFitsAnIntOfBitsAndNoMore()
        {
            // 268,435,455 bytes of one 8-bit colour is the widest row: its bits fill an int.
            Assert.Equal(268_435_455, PngPredictor.CalculateRowLength(1, 8, 268_435_455));

            // One more sample takes the bits past an int; so does a row of exactly int.MaxValue
            // bytes, whose stride used to wrap negative.
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(1, 8, 268_435_456));
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(1, 8, int.MaxValue));

            // Two 1-bit colours over 2^30 columns made the sample count wrap in the TIFF decoder.
            Assert.Throws<ArgumentOutOfRangeException>(() => PngPredictor.CalculateRowLength(2, 1, 1_073_741_824));
        }

        [Fact]
        public void AnImplausibleHeightTakesTheOrdinaryPath()
        {
            // Two rows of one byte with PNG filter type 0, stored uncompressed in a zlib frame.
            byte[] compressed = [0x78, 0x01, 0x01, 0x04, 0x00, 0xFB, 0xFF, 0x00, 0x05, 0x00, 0x07, 0x00, 0x1A, 0x00, 0x0D];
            var expected = new byte[] { 5, 7 };

            var filter = new FlateFilter();

            // A believable height decodes straight into the result; two billion rows for fifteen
            // bytes of input may not size anything and decode the ordinary way, to the same bytes.
            Assert.Equal(expected, filter.Decode(compressed, ImageDictionary(2), DefaultFilterProvider.Instance, 0).ToArray());
            Assert.Equal(expected, filter.Decode(compressed, ImageDictionary(2_000_000_000), DefaultFilterProvider.Instance, 0).ToArray());
        }

        private static DictionaryToken ImageDictionary(int height) => StreamDictionary(NameToken.FlateDecode, [(NameToken.Predictor, 12), (NameToken.Columns, 1)], (NameToken.Height, height));

        [Fact]
        public void TheParametersDefaultAsTable8Says()
        {
            // ISO 32000, Table 8: Predictor 1, Colors 1, BitsPerComponent 8, Columns 1.
            var (predictor, colors, bitsPerComponent, columns) = PngPredictor.Parameters.Read(Dictionary());

            Assert.Equal((1, 1, 8, 1), (predictor, colors, bitsPerComponent, columns));

            var stated = PngPredictor.Parameters.Read(Dictionary((NameToken.Predictor, 15), (NameToken.Colors, 3), (NameToken.BitsPerComponent, 16), (NameToken.Columns, 640)));

            Assert.Equal((15, 3, 16, 640), (stated.Predictor, stated.Colors, stated.BitsPerComponent, stated.Columns));
        }

        [Fact]
        public void AnInvalidStreamThroughTheFlateFilterYieldsNothing()
        {
            // The filter turns the refusal into an empty result, as it does for damaged data.
            var filter = new FlateFilter();
            var dictionary = StreamDictionary(NameToken.FlateDecode, [(NameToken.Predictor, 12), (NameToken.Colors, 32), (NameToken.BitsPerComponent, 1073741824), (NameToken.Columns, 536870912)]);

            // A valid zlib stream of a few bytes: header, one stored block "abc", checksum.
            byte[] compressed = [0x78, 0x9C, 0x01, 0x03, 0x00, 0xFC, 0xFF, 0x61, 0x62, 0x63, 0x02, 0x4D, 0x01, 0x27];

            var decoded = filter.Decode(compressed, dictionary, DefaultFilterProvider.Instance, 0);

            Assert.True(decoded.IsEmpty);
        }
    }
}
