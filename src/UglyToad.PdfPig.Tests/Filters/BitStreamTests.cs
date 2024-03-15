namespace UglyToad.PdfPig.Tests.Filters
{
    using PdfPig.Filters;

    public class BitStreamTests
    {
        private readonly byte[] data = {
            0b00101001,
            0b11011100,
            0b01000110,
            0b11111011,
            0b00101010,
            0b11010111,
            0b10010001,
            0b11011011,
            0b11110000,
            0b00010111,
            0b10101011
        };

        [Fact]
        public void GetNumbers()
        {
            var bitStream = new BitStream(data);

            var first = bitStream.Get(9);
            var second = bitStream.Get(9);
            var third = bitStream.Get(11);
            var fourth = bitStream.Get(5);
            var fifth = bitStream.Get(17);

            Assert.Equal(0b001010011, first);
            Assert.Equal(0b101110001, second);
            Assert.Equal(0b00011011111, third);
            Assert.Equal(0b01100, fourth);
            Assert.Equal(0b10101011010111100, fifth);
        }

        [Fact]
        public void GetNumbersCrossingBoundaries()
        {
            var bitStream = new BitStream(data);

            var first = bitStream.Get(13);
            var second = bitStream.Get(15);
            var third = bitStream.Get(13);

            Assert.Equal(0b0010100111011, first);
            Assert.Equal(0b100010001101111, second);
            Assert.Equal(0b1011001010101, third);
        }

        [Fact]
        public void GetNumbersUntilOffsetResets()
        {
            var bitStream = new BitStream(data);

            var first = bitStream.Get(9);
            var second = bitStream.Get(9);
            var third = bitStream.Get(9);
            var fourth = bitStream.Get(9);
            var fifth = bitStream.Get(9);
            var sixth = bitStream.Get(9);
            var seventh = bitStream.Get(9);
            var eighth = bitStream.Get(9);
            var ninth = bitStream.Get(9);

            var end = bitStream.Get(7);

            Assert.Equal(0b001010011, first);
            Assert.Equal(0b101110001, second);
            Assert.Equal(0b000110111, third);
            Assert.Equal(0b110110010, fourth);
            Assert.Equal(0b101011010, fifth);
            Assert.Equal(0b111100100, sixth);
            Assert.Equal(0b011101101, seventh);
            Assert.Equal(0b111110000, eighth);
            Assert.Equal(0b000101111, ninth);

            Assert.Equal(0b0101011, end);
        }
    }
}
