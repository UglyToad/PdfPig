namespace UglyToad.PdfPig.Tests.Fonts.TrueType.Parser
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Fonts.TrueType.Tables;
    using PdfPig.IO;
    using Xunit;

    public class TrueTypeFontParserTests
    {
        private static byte[] GetFileBytes(string name)
        {
            var manifestFiles = typeof(TrueTypeFontParserTests).Assembly.GetManifestResourceNames();

            var match = manifestFiles.Single(x => x.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0);

            using (var memoryStream = new MemoryStream())
            using (var stream = typeof(TrueTypeFontParserTests).Assembly.GetManifestResourceStream(match))
            {
                stream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }

        private readonly TrueTypeFontParser parser = new TrueTypeFontParser();

        [Fact]
        public void ParseRegularRoboto()
        {
            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            Assert.Equal(1, font.Version);

            Assert.Equal(1, font.HeaderTable.Version);
            Assert.Equal(1, font.HeaderTable.Revision);

            Assert.Equal(1142661421, font.HeaderTable.CheckSumAdjustment);
            Assert.Equal(1594834165, font.HeaderTable.MagicNumber);

            Assert.Equal(9, font.HeaderTable.Flags);

            Assert.Equal(2048, font.HeaderTable.UnitsPerEm);

            Assert.Equal(2008, font.HeaderTable.Created.Year);
            Assert.Equal(10, font.HeaderTable.Created.Month);
            Assert.Equal(13, font.HeaderTable.Created.Day);
            Assert.Equal(12, font.HeaderTable.Created.Hour);
            Assert.Equal(29, font.HeaderTable.Created.Minute);
            Assert.Equal(34, font.HeaderTable.Created.Second);

            Assert.Equal(2011, font.HeaderTable.Modified.Year);
            Assert.Equal(12, font.HeaderTable.Modified.Month);
            Assert.Equal(31, font.HeaderTable.Modified.Day);
            Assert.Equal(5, font.HeaderTable.Modified.Hour);
            Assert.Equal(13, font.HeaderTable.Modified.Minute);
            Assert.Equal(10, font.HeaderTable.Modified.Second);

            Assert.Equal(-980, font.HeaderTable.Bounds.Left);
            Assert.Equal(-555, font.HeaderTable.Bounds.Bottom);

            Assert.Equal(2396, font.HeaderTable.Bounds.Right);
            Assert.Equal(2163, font.HeaderTable.Bounds.Top);

            Assert.Equal(HeaderTable.HeaderMacStyle.None, font.HeaderTable.MacStyle);
            Assert.Equal(9, font.HeaderTable.LowestRecommendedPpem);

            Assert.Equal(HeaderTable.FontDirection.StronglyLeftToRightWithNeutrals, font.HeaderTable.FontDirectionHint);

            Assert.Equal(0, font.HeaderTable.IndexToLocFormat);
            Assert.Equal(0, font.HeaderTable.GlyphDataFormat);
        }

        [Fact]
        public void RobotoHeaderReadCorrectly()
        {
            var data = new[]
            {
                // key, offset, length, checksum
                "DSIG 158596 8 1",
                "GDEF 316 72 408950881",
                "GPOS 388 35744 355098641",
                "GSUB 36132 662 3357985284",
                "OS/2 36796 96 3097700805",
                "cmap 36892 1750 298470964",
                "cvt  156132 38 119085513",
                "fpgm 156172 2341 2494100564",
                "gasp 156124 8 16",
                "glyf 38644 88820 3302131736",
                "head 127464 54 346075833",
                "hhea 127520 36 217516755",
                "hmtx 127556 4148 1859679943",
                "kern 131704 12306 2002873469",
                "loca 144012 2076 77421448",
                "maxp 146088 32 89459325",
                "name 146120 830 44343214",
                "post 146952 9171 3638780613",
                "prep 158516 77 251381919"
            };

            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            foreach (var s in data)
            {
                var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var name = parts[0];

                if (name == "cvt")
                {
                    name = "cvt ";
                }

                var match = font.TableHeaders[name];

                var offset = long.Parse(parts[1]);
                var length = long.Parse(parts[2]);
                var checksum = long.Parse(parts[3]);

                Assert.Equal(offset, match.Offset);
                Assert.Equal(length, match.Length);
                Assert.Equal(checksum, match.CheckSum);
            }
        }

        [Fact]
        public void ParseEmbeddedSimpleGoogleDocssGautmi()
        {
            var bytes = GetFileBytes("google-simple-doc");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            parser.Parse(input);
        }
    }
}
