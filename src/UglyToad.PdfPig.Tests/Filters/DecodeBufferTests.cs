namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Collections.Generic;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class DecodeBufferTests
    {
        private const int Factor = 4;
        private const int Minimum = 4096;
        private const int Deflate = 1032;
        private const int Lzw = 1400;

        [Fact]
        public void WithoutAStatedLengthTheFactorDecides()
        {
            Assert.Equal(Minimum, DecodeBuffer.Capacity(500, Dictionary(), Factor, Minimum, Deflate));
            Assert.Equal(40_000, DecodeBuffer.Capacity(10_000, Dictionary(), Factor, Minimum, Deflate));
        }

        [Fact]
        public void AStatedLengthIsHonouredWithRoomToFinish()
        {
            var capacity = DecodeBuffer.Capacity(30_000, Dictionary((NameToken.Dl, 100_000)), Factor, Minimum, Deflate);

            Assert.InRange(capacity, 100_001, 100_000 + 4096);
        }

        [Fact]
        public void TheFontProgramLengthsAddUp()
        {
            var capacity = DecodeBuffer.Capacity(40_000, Dictionary((NameToken.Length1, 50_000), (NameToken.Length2, 20_000)), Factor, Minimum, Deflate);

            Assert.InRange(capacity, 70_001, 70_000 + 4096);
        }

        [Fact]
        public void TheDecodedLengthOutranksTheFontProgramLengths()
        {
            var capacity = DecodeBuffer.Capacity(30_000, Dictionary((NameToken.Dl, 100_000), (NameToken.Length1, 5_000_000)), Factor, Minimum, Deflate);

            Assert.InRange(capacity, 100_001, 100_000 + 4096);
        }

        [Fact]
        public void AStatedLengthBeyondWhatDeflateCanExpandToIsIgnored()
        {
            // 1,000 bytes cannot inflate to 5 MB; the dictionary is wrong, and the factor decides.
            Assert.Equal(Minimum, DecodeBuffer.Capacity(1_000, Dictionary((NameToken.Dl, 5_000_000)), Factor, Minimum, Deflate));
        }

        [Fact]
        public void AStatedLengthAboveTheCeilingIsIgnoredHoweverPlausible()
        {
            // A megabyte can inflate to two gigabytes, but a number in the file may not choose an
            // allocation of that size before a byte has been decoded.
            Assert.Equal(4_000_000, DecodeBuffer.Capacity(1_000_000, Dictionary((NameToken.Dl, 2_000_000_000)), Factor, Minimum, Deflate));

            var justAbove = (int)DecodeBuffer.MaximumStatedLength + 1;
            Assert.Equal(4_000_000, DecodeBuffer.Capacity(1_000_000, Dictionary((NameToken.Dl, justAbove)), Factor, Minimum, Deflate));

            var justBelow = (int)DecodeBuffer.MaximumStatedLength - 1;
            Assert.InRange(DecodeBuffer.Capacity(1_000_000, Dictionary((NameToken.Dl, justBelow)), Factor, Minimum, Deflate), justBelow + 1, justBelow + 4096);
        }

        [Fact]
        public void TheBoundIsTheFilterOwn()
        {
            // 1,200 to one is beyond deflate but within LZW, and Brotli has no bound but the ceiling.
            var stated = 1_200_000;

            Assert.Equal(Minimum, DecodeBuffer.Capacity(1_000, Dictionary((NameToken.Dl, stated)), Factor, Minimum, Deflate));
            Assert.InRange(DecodeBuffer.Capacity(1_000, Dictionary((NameToken.Dl, stated)), Factor, Minimum, Lzw), stated + 1, stated + 4096);
            Assert.InRange(DecodeBuffer.Capacity(100, Dictionary((NameToken.Dl, 20_000_000)), Factor, Minimum, DecodeBuffer.UnboundedExpansion), 20_000_001, 20_000_000 + 4096);
            Assert.Equal(Minimum, DecodeBuffer.Capacity(100, Dictionary((NameToken.Dl, 2_000_000_000)), Factor, Minimum, DecodeBuffer.UnboundedExpansion));
        }

        [Fact]
        public void TheBoundAloneLeavesTinyStreamsTheirStatedLength()
        {
            // 20 bytes of deflate can inflate to 20,640; a statement within that is taken as it is.
            Assert.InRange(DecodeBuffer.Capacity(20, Dictionary((NameToken.Dl, 15_000)), Factor, Minimum, Deflate), 15_001, 15_000 + 4096);

            // Nothing inflates to something: a statement on empty input is ignored.
            Assert.Equal(Minimum, DecodeBuffer.Capacity(0, Dictionary((NameToken.Dl, 100)), Factor, Minimum, Deflate));
        }

        [Fact]
        public void ZeroAndNegativeStatedLengthsAreIgnored()
        {
            Assert.Equal(Minimum, DecodeBuffer.Capacity(500, Dictionary((NameToken.Dl, 0)), Factor, Minimum, Deflate));
            Assert.Equal(Minimum, DecodeBuffer.Capacity(500, Dictionary((NameToken.Length1, -7)), Factor, Minimum, Deflate));
        }

        private static DictionaryToken Dictionary(params (NameToken Key, int Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();

            foreach (var (key, value) in entries)
            {
                data[key] = new NumericToken(value);
            }

            return new DictionaryToken(data);
        }
    }
}
