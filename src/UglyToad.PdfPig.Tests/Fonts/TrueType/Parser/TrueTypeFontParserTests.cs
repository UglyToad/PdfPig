namespace UglyToad.PdfPig.Tests.Fonts.TrueType.Parser
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Fonts.TrueType.Tables;
    using PdfPig.IO;
    using Xunit;

    public class TrueTypeFontParserTests
    {
        private static byte[] GetFileBytes(string name)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");

            name = name.EndsWith(".ttf") || name.EndsWith(".txt") ? name : name + ".ttf";

            var file = Path.Combine(path, name);

            return File.ReadAllBytes(file);
        }

        private readonly TrueTypeFontParser parser = new TrueTypeFontParser();

        [Fact]
        public void ParseRegularRoboto()
        {
            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            Assert.Equal(1, font.Version);

            Assert.Equal(1, font.TableRegister.HeaderTable.Version);
            Assert.Equal(1, font.TableRegister.HeaderTable.Revision);

            Assert.Equal(1142661421, font.TableRegister.HeaderTable.CheckSumAdjustment);
            Assert.Equal(1594834165, font.TableRegister.HeaderTable.MagicNumber);

            Assert.Equal(9, font.TableRegister.HeaderTable.Flags);

            Assert.Equal(2048, font.TableRegister.HeaderTable.UnitsPerEm);

            Assert.Equal(2008, font.TableRegister.HeaderTable.Created.Year);
            Assert.Equal(10, font.TableRegister.HeaderTable.Created.Month);
            Assert.Equal(13, font.TableRegister.HeaderTable.Created.Day);
            Assert.Equal(12, font.TableRegister.HeaderTable.Created.Hour);
            Assert.Equal(29, font.TableRegister.HeaderTable.Created.Minute);
            Assert.Equal(34, font.TableRegister.HeaderTable.Created.Second);

            Assert.Equal(2011, font.TableRegister.HeaderTable.Modified.Year);
            Assert.Equal(12, font.TableRegister.HeaderTable.Modified.Month);
            Assert.Equal(31, font.TableRegister.HeaderTable.Modified.Day);
            Assert.Equal(5, font.TableRegister.HeaderTable.Modified.Hour);
            Assert.Equal(13, font.TableRegister.HeaderTable.Modified.Minute);
            Assert.Equal(10, font.TableRegister.HeaderTable.Modified.Second);

            Assert.Equal(-980, font.TableRegister.HeaderTable.Bounds.Left);
            Assert.Equal(-555, font.TableRegister.HeaderTable.Bounds.Bottom);

            Assert.Equal(2396, font.TableRegister.HeaderTable.Bounds.Right);
            Assert.Equal(2163, font.TableRegister.HeaderTable.Bounds.Top);

            Assert.Equal(HeaderTable.HeaderMacStyle.None, font.TableRegister.HeaderTable.MacStyle);
            Assert.Equal(9, font.TableRegister.HeaderTable.LowestRecommendedPpem);

            Assert.Equal(HeaderTable.FontDirection.StronglyLeftToRightWithNeutrals, font.TableRegister.HeaderTable.FontDirectionHint);

            Assert.Equal(0, font.TableRegister.HeaderTable.IndexToLocFormat);
            Assert.Equal(0, font.TableRegister.HeaderTable.GlyphDataFormat);
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

                var offset = long.Parse(parts[1], CultureInfo.InvariantCulture);
                var length = long.Parse(parts[2], CultureInfo.InvariantCulture);
                var checksum = long.Parse(parts[3], CultureInfo.InvariantCulture);

                Assert.Equal(offset, match.Offset);
                Assert.Equal(length, match.Length);
                Assert.Equal(checksum, match.CheckSum);
            }
        }

        [Fact]
        public void ParseSimpleGoogleDocssGautmi()
        {
            var bytes = GetFileBytes("google-simple-doc");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            Assert.NotNull(font.TableRegister.GlyphTable);
        }

        [Fact]
        public void ParseAndadaRegular()
        {
            var bytes = GetFileBytes("Andada-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            Assert.NotNull(font.TableRegister.GlyphTable);

            var name = font.Name;

            Assert.Equal("Andada Regular", name);
        }

        [Fact]
        public void ParsePMingLiU()
        {
            var bytes = GetFileBytes("PMingLiU");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);
        }

        [Fact]
        public void ReadsRobotoGlyphSizesCorrectly()
        {
            var regex = new Regex(@"\?: Width (?<width>\d+), Height: (?<height>\d+), Points: (?<points>\d+)");

            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            var robotoGlyphs = Encoding.ASCII.GetString(GetFileBytes("Roboto-Regular.GlyphData.txt"));
            var lines = robotoGlyphs.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var match = regex.Match(lines[i]);

                var width = decimal.Parse(match.Groups["width"].Value, CultureInfo.InvariantCulture);
                var height = decimal.Parse(match.Groups["height"].Value, CultureInfo.InvariantCulture);
                var points = int.Parse(match.Groups["points"].Value, CultureInfo.InvariantCulture);

                var glyph = font.TableRegister.GlyphTable.Glyphs[i];

                // Vendor data ignores the empty glyph bounds.
                if (width == 0 && height == 0)
                {
                    continue;
                }

                if (i != 30)
                {
                    Assert.Equal(width, glyph.Bounds.Width);
                }
                
                Assert.Equal(height, glyph.Bounds.Height);
                Assert.Equal(points, glyph.Points.Length);
            }
        }
    }
}

