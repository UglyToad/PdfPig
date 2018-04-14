namespace UglyToad.PdfPig.Tests.Fonts.TrueType.Parser
{
    using System;
    using System.IO;
    using System.Linq;
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

        [Fact]
        public void ReadsRobotoGlyphSizesCorrectly()
        {
            var regex = new Regex(@"\?: Width (?<width>\d+), Height: (?<height>\d+), Points: (?<points>\d+)");

            var bytes = GetFileBytes("Roboto-Regular");

            var input = new TrueTypeDataBytes(new ByteArrayInputBytes(bytes));

            var font = parser.Parse(input);

            var lines = RobotoGlyphs.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var match = regex.Match(lines[i]);

                var width = decimal.Parse(match.Groups["width"].Value);
                var height = decimal.Parse(match.Groups["height"].Value);
                var points = int.Parse(match.Groups["points"].Value);

                var glyph = font.GlyphTable.Glyphs[i];

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

        #region Test Data
        private const string RobotoGlyphs = @"?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 198, Height: 1456, Points: 8
?: Width 507, Height: 520, Points: 11
?: Width 1116, Height: 1456, Points: 32
?: Width 935, Height: 1901, Points: 45
?: Width 1307, Height: 1498, Points: 60
?: Width 1168, Height: 1498, Points: 58
?: Width 198, Height: 505, Points: 6
?: Width 537, Height: 2099, Points: 18
?: Width 537, Height: 2099, Points: 18
?: Width 707, Height: 691, Points: 15
?: Width 998, Height: 1060, Points: 12
?: Width 267, Height: 479, Points: 6
?: Width 590, Height: 154, Points: 4
?: Width 197, Height: 202, Points: 4
?: Width 775, Height: 1581, Points: 4
?: Width 927, Height: 1498, Points: 28
?: Width 515, Height: 1477, Points: 6
?: Width 921, Height: 1477, Points: 27
?: Width 924, Height: 1498, Points: 43
?: Width 1022, Height: 1456, Points: 16
?: Width 891, Height: 1477, Points: 32
?: Width 927, Height: 1498, Points: 40
?: Width 966, Height: 1456, Points: 13
?: Width 948, Height: 1498, Points: 48
?: Width 923, Height: 1498, Points: 40
?: Width 197, Height: 1078, Points: 8
?: Width 267, Height: 1336, Points: 10
?: Width 816, Height: 906, Points: 10
?: Width 834, Height: 580, Points: 8
?: Width 856, Height: 906, Points: 10
?: Width 828, Height: 1477, Points: 31
?: Width 1653, Height: 1884, Points: 68
?: Width 1208, Height: 1456, Points: 12
?: Width 1059, Height: 1456, Points: 34
?: Width 1097, Height: 1498, Points: 30
?: Width 1090, Height: 1456, Points: 20
?: Width 897, Height: 1456, Points: 12
?: Width 913, Height: 1456, Points: 10
?: Width 1096, Height: 1498, Points: 33
?: Width 1101, Height: 1456, Points: 12
?: Width 198, Height: 1456, Points: 4
?: Width 882, Height: 1477, Points: 17
?: Width 1111, Height: 1456, Points: 15
?: Width 907, Height: 1456, Points: 6
?: Width 1438, Height: 1456, Points: 16
?: Width 1101, Height: 1456, Points: 12
?: Width 1169, Height: 1498, Points: 28
?: Width 1049, Height: 1456, Points: 20
?: Width 1257, Height: 1620, Points: 34
?: Width 1052, Height: 1455, Points: 36
?: Width 1034, Height: 1498, Points: 40
?: Width 1151, Height: 1456, Points: 8
?: Width 1097, Height: 1477, Points: 18
?: Width 1251, Height: 1456, Points: 10
?: Width 1690, Height: 1456, Points: 22
?: Width 1172, Height: 1456, Points: 12
?: Width 1210, Height: 1456, Points: 9
?: Width 1036, Height: 1456, Points: 10
?: Width 385, Height: 1976, Points: 8
?: Width 794, Height: 1581, Points: 4
?: Width 386, Height: 1976, Points: 8
?: Width 731, Height: 727, Points: 10
?: Width 923, Height: 154, Points: 4
?: Width 408, Height: 266, Points: 5
?: Width 905, Height: 1123, Points: 44
?: Width 924, Height: 1581, Points: 32
?: Width 888, Height: 1123, Points: 30
?: Width 915, Height: 1581, Points: 32
?: Width 897, Height: 1123, Points: 32
?: Width 657, Height: 1581, Points: 24
?: Width 916, Height: 1539, Points: 44
?: Width 881, Height: 1560, Points: 20
?: Width 197, Height: 1560, Points: 8
?: Width 436, Height: 1997, Points: 20
?: Width 891, Height: 1560, Points: 13
?: Width 197, Height: 1560, Points: 4
?: Width 1504, Height: 1102, Points: 36
?: Width 878, Height: 1102, Points: 20
?: Width 969, Height: 1123, Points: 28
?: Width 922, Height: 1518, Points: 32
?: Width 904, Height: 1518, Points: 32
?: Width 539, Height: 1102, Points: 16
?: Width 866, Height: 1123, Points: 40
?: Width 584, Height: 1364, Points: 24
?: Width 881, Height: 1103, Points: 20
?: Width 950, Height: 1082, Points: 10
?: Width 1455, Height: 1082, Points: 22
?: Width 934, Height: 1082, Points: 12
?: Width 974, Height: 1519, Points: 22
?: Width 860, Height: 1082, Points: 10
?: Width 608, Height: 1961, Points: 31
?: Width 197, Height: 1726, Points: 4
?: Width 609, Height: 1961, Points: 31
?: Width 1136, Height: 402, Points: 26
?: Width 0, Height: 0, Points: 0
?: Width 197, Height: 1456, Points: 8
?: Width 889, Height: 1563, Points: 36
?: Width 1041, Height: 1477, Points: 35
?: Width 1266, Height: 1292, Points: 48
?: Width 1169, Height: 1456, Points: 23
?: Width 197, Height: 1726, Points: 8
?: Width 1058, Height: 1972, Points: 70
?: Width 689, Height: 200, Points: 8
?: Width 1419, Height: 1497, Points: 54
?: Width 667, Height: 785, Points: 44
?: Width 778, Height: 797, Points: 14
?: Width 835, Height: 427, Points: 6
?: Width 590, Height: 154, Points: 4
?: Width 1419, Height: 1497, Points: 60
?: Width 721, Height: 146, Points: 4
?: Width 509, Height: 518, Points: 24
?: Width 916, Height: 1263, Points: 16
?: Width 601, Height: 812, Points: 27
?: Width 634, Height: 823, Points: 43
?: Width 417, Height: 266, Points: 5
?: Width 857, Height: 1498, Points: 22
?: Width 773, Height: 1456, Points: 11
?: Width 198, Height: 212, Points: 4
?: Width 312, Height: 435, Points: 16
?: Width 301, Height: 812, Points: 6
?: Width 691, Height: 786, Points: 28
?: Width 783, Height: 760, Points: 28
?: Width 1322, Height: 1476, Points: 26
?: Width 1341, Height: 1476, Points: 37
?: Width 1573, Height: 1479, Points: 63
?: Width 827, Height: 1477, Points: 31
?: Width 1208, Height: 1867, Points: 17
?: Width 1208, Height: 1863, Points: 17
?: Width 1208, Height: 1864, Points: 21
?: Width 1208, Height: 1876, Points: 32
?: Width 1208, Height: 1805, Points: 20
?: Width 1208, Height: 1903, Points: 36
?: Width 1910, Height: 1456, Points: 20
?: Width 1097, Height: 1921, Points: 46
?: Width 897, Height: 1867, Points: 17
?: Width 897, Height: 1863, Points: 17
?: Width 897, Height: 1864, Points: 21
?: Width 897, Height: 1805, Points: 20
?: Width 423, Height: 1867, Points: 9
?: Width 426, Height: 1863, Points: 9
?: Width 614, Height: 1864, Points: 13
?: Width 689, Height: 1805, Points: 12
?: Width 1258, Height: 1456, Points: 28
?: Width 1101, Height: 1876, Points: 32
?: Width 1169, Height: 1909, Points: 33
?: Width 1169, Height: 1905, Points: 33
?: Width 1169, Height: 1906, Points: 37
?: Width 1169, Height: 1918, Points: 48
?: Width 1169, Height: 1847, Points: 36
?: Width 905, Height: 920, Points: 12
?: Width 1169, Height: 1611, Points: 50
?: Width 1097, Height: 1888, Points: 23
?: Width 1097, Height: 1884, Points: 23
?: Width 1097, Height: 1885, Points: 27
?: Width 1097, Height: 1826, Points: 26
?: Width 1210, Height: 1862, Points: 14
?: Width 958, Height: 1456, Points: 22
?: Width 999, Height: 1576, Points: 40
?: Width 905, Height: 1566, Points: 49
?: Width 905, Height: 1562, Points: 49
?: Width 905, Height: 1563, Points: 53
?: Width 905, Height: 1575, Points: 64
?: Width 905, Height: 1504, Points: 52
?: Width 905, Height: 1602, Points: 68
?: Width 1602, Height: 1123, Points: 67
?: Width 888, Height: 1546, Points: 46
?: Width 897, Height: 1567, Points: 37
?: Width 897, Height: 1563, Points: 37
?: Width 897, Height: 1564, Points: 41
?: Width 897, Height: 1505, Points: 40
?: Width 422, Height: 1524, Points: 9
?: Width 426, Height: 1520, Points: 9
?: Width 614, Height: 1521, Points: 13
?: Width 689, Height: 1462, Points: 12
?: Width 1000, Height: 1538, Points: 46
?: Width 878, Height: 1554, Points: 40
?: Width 969, Height: 1566, Points: 33
?: Width 969, Height: 1562, Points: 33
?: Width 969, Height: 1563, Points: 37
?: Width 969, Height: 1575, Points: 48
?: Width 969, Height: 1504, Points: 36
?: Width 998, Height: 1022, Points: 12
?: Width 969, Height: 1344, Points: 50
?: Width 881, Height: 1545, Points: 25
?: Width 881, Height: 1541, Points: 25
?: Width 881, Height: 1542, Points: 29
?: Width 881, Height: 1483, Points: 28
?: Width 974, Height: 1957, Points: 27
?: Width 922, Height: 1976, Points: 32
?: Width 974, Height: 1899, Points: 30
?: Width 1208, Height: 1786, Points: 16
?: Width 905, Height: 1485, Points: 48
?: Width 1208, Height: 1870, Points: 28
?: Width 905, Height: 1569, Points: 60
?: Width 1263, Height: 1888, Points: 32
?: Width 960, Height: 1534, Points: 64
?: Width 1097, Height: 1905, Points: 35
?: Width 888, Height: 1562, Points: 35
?: Width 1097, Height: 1906, Points: 39
?: Width 888, Height: 1563, Points: 39
?: Width 1097, Height: 1847, Points: 34
?: Width 888, Height: 1504, Points: 34
?: Width 1097, Height: 1907, Points: 39
?: Width 888, Height: 1564, Points: 39
?: Width 1090, Height: 1865, Points: 29
?: Width 1211, Height: 1581, Points: 38
?: Width 1258, Height: 1456, Points: 28
?: Width 1112, Height: 1581, Points: 40
?: Width 897, Height: 1786, Points: 16
?: Width 897, Height: 1486, Points: 36
?: Width 897, Height: 1870, Points: 28
?: Width 897, Height: 1570, Points: 48
?: Width 897, Height: 1805, Points: 16
?: Width 897, Height: 1505, Points: 36
?: Width 897, Height: 1888, Points: 32
?: Width 897, Height: 1474, Points: 52
?: Width 897, Height: 1865, Points: 21
?: Width 897, Height: 1565, Points: 41
?: Width 1096, Height: 1906, Points: 42
?: Width 916, Height: 1979, Points: 53
?: Width 1096, Height: 1912, Points: 49
?: Width 916, Height: 1985, Points: 60
?: Width 1096, Height: 1847, Points: 37
?: Width 916, Height: 1920, Points: 48
?: Width 1096, Height: 1972, Points: 39
?: Width 916, Height: 2085, Points: 50
?: Width 1101, Height: 1864, Points: 21
?: Width 881, Height: 1863, Points: 29
?: Width 1392, Height: 1456, Points: 24
?: Width 1052, Height: 1560, Points: 28
?: Width 694, Height: 1876, Points: 24
?: Width 694, Height: 1533, Points: 24
?: Width 721, Height: 1786, Points: 8
?: Width 721, Height: 1444, Points: 8
?: Width 599, Height: 1870, Points: 20
?: Width 599, Height: 1527, Points: 20
?: Width 399, Height: 1888, Points: 24
?: Width 399, Height: 1992, Points: 28
?: Width 218, Height: 1805, Points: 8
?: Width 197, Height: 1082, Points: 4
?: Width 1345, Height: 1477, Points: 21
?: Width 727, Height: 1997, Points: 28
?: Width 1083, Height: 1873, Points: 26
?: Width 647, Height: 1939, Points: 25
?: Width 1111, Height: 1934, Points: 21
?: Width 891, Height: 2036, Points: 19
?: Width 935, Height: 1082, Points: 15
?: Width 907, Height: 1800, Points: 11
?: Width 426, Height: 1875, Points: 9
?: Width 907, Height: 1932, Points: 12
?: Width 246, Height: 2036, Points: 10
?: Width 907, Height: 1457, Points: 12
?: Width 526, Height: 1560, Points: 10
?: Width 907, Height: 1456, Points: 10
?: Width 458, Height: 1560, Points: 8
?: Width 1030, Height: 1456, Points: 14
?: Width 489, Height: 1560, Points: 12
?: Width 1101, Height: 1863, Points: 17
?: Width 878, Height: 1541, Points: 25
?: Width 1101, Height: 1932, Points: 18
?: Width 878, Height: 1578, Points: 26
?: Width 1101, Height: 1865, Points: 21
?: Width 878, Height: 1543, Points: 29
?: Width 1054, Height: 1560, Points: 26
?: Width 1101, Height: 1893, Points: 25
?: Width 870, Height: 1539, Points: 32
?: Width 1169, Height: 1828, Points: 32
?: Width 969, Height: 1485, Points: 32
?: Width 1169, Height: 1912, Points: 44
?: Width 969, Height: 1569, Points: 44
?: Width 1169, Height: 1909, Points: 39
?: Width 991, Height: 1566, Points: 39
?: Width 1698, Height: 1498, Points: 38
?: Width 1677, Height: 1123, Points: 58
?: Width 1052, Height: 1863, Points: 41
?: Width 597, Height: 1541, Points: 21
?: Width 1052, Height: 1931, Points: 42
?: Width 575, Height: 1578, Points: 22
?: Width 1052, Height: 1865, Points: 45
?: Width 630, Height: 1543, Points: 25
?: Width 1034, Height: 1905, Points: 45
?: Width 866, Height: 1562, Points: 45
?: Width 1034, Height: 1906, Points: 49
?: Width 866, Height: 1563, Points: 49
?: Width 1034, Height: 1921, Points: 56
?: Width 866, Height: 1545, Points: 56
?: Width 1034, Height: 1907, Points: 49
?: Width 866, Height: 1564, Points: 49
?: Width 1151, Height: 1932, Points: 14
?: Width 584, Height: 1829, Points: 30
?: Width 1151, Height: 1864, Points: 17
?: Width 729, Height: 1608, Points: 30
?: Width 1151, Height: 1456, Points: 16
?: Width 641, Height: 1364, Points: 32
?: Width 1097, Height: 1897, Points: 38
?: Width 881, Height: 1554, Points: 40
?: Width 1097, Height: 1807, Points: 22
?: Width 881, Height: 1465, Points: 24
?: Width 1097, Height: 1891, Points: 34
?: Width 881, Height: 1548, Points: 36
?: Width 1097, Height: 1924, Points: 42
?: Width 881, Height: 1581, Points: 44
?: Width 1097, Height: 1888, Points: 29
?: Width 947, Height: 1545, Points: 31
?: Width 1097, Height: 1888, Points: 38
?: Width 935, Height: 1514, Points: 40
?: Width 1690, Height: 1864, Points: 31
?: Width 1455, Height: 1521, Points: 31
?: Width 1210, Height: 1863, Points: 18
?: Width 974, Height: 1958, Points: 31
?: Width 1210, Height: 1804, Points: 17
?: Width 1036, Height: 1863, Points: 15
?: Width 860, Height: 1520, Points: 15
?: Width 1036, Height: 1805, Points: 14
?: Width 860, Height: 1462, Points: 14
?: Width 1036, Height: 1865, Points: 19
?: Width 860, Height: 1522, Points: 19
?: Width 488, Height: 1581, Points: 16
?: Width 727, Height: 2018, Points: 36
?: Width 1427, Height: 1674, Points: 38
?: Width 1159, Height: 1244, Points: 38
?: Width 1476, Height: 1612, Points: 28
?: Width 1247, Height: 1248, Points: 30
?: Width 655, Height: 1940, Points: 25
?: Width 1208, Height: 2085, Points: 41
?: Width 905, Height: 1784, Points: 73
?: Width 1910, Height: 1863, Points: 25
?: Width 1602, Height: 1563, Points: 72
?: Width 1169, Height: 2019, Points: 55
?: Width 969, Height: 1675, Points: 55
?: Width 1034, Height: 1973, Points: 46
?: Width 866, Height: 1597, Points: 46
?: Width 436, Height: 1519, Points: 16
?: Width 198, Height: 539, Points: 6
?: Width 614, Height: 263, Points: 9
?: Width 630, Height: 263, Points: 9
?: Width 599, Height: 268, Points: 16
?: Width 218, Height: 201, Points: 4
?: Width 349, Height: 333, Points: 24
?: Width 399, Height: 489, Points: 20
?: Width 694, Height: 274, Points: 20
?: Width 742, Height: 266, Points: 11
?: Width 310, Height: 295, Points: 24
?: Width 284, Height: 351, Points: 5
?: Width 285, Height: 353, Points: 5
?: Width 694, Height: 274, Points: 20
?: Width 312, Height: 412, Points: 16
?: Width 742, Height: 266, Points: 11
?: Width 218, Height: 200, Points: 4
?: Width 238, Height: 271, Points: 4
?: Width 708, Height: 474, Points: 12
?: Width 1208, Height: 1543, Points: 16
?: Width 198, Height: 212, Points: 4
?: Width 1195, Height: 1545, Points: 16
?: Width 1350, Height: 1545, Points: 16
?: Width 453, Height: 1544, Points: 8
?: Width 1222, Height: 1564, Points: 32
?: Width 1446, Height: 1543, Points: 13
?: Width 1181, Height: 1543, Points: 40
?: Width 708, Height: 1678, Points: 28
?: Width 1208, Height: 1456, Points: 12
?: Width 1059, Height: 1456, Points: 34
?: Width 893, Height: 1456, Points: 6
?: Width 1362, Height: 1456, Points: 8
?: Width 897, Height: 1456, Points: 12
?: Width 1036, Height: 1456, Points: 10
?: Width 1101, Height: 1456, Points: 12
?: Width 1169, Height: 1498, Points: 32
?: Width 198, Height: 1456, Points: 4
?: Width 1111, Height: 1456, Points: 15
?: Width 1238, Height: 1456, Points: 8
?: Width 1438, Height: 1456, Points: 16
?: Width 1101, Height: 1456, Points: 12
?: Width 937, Height: 1456, Points: 12
?: Width 1169, Height: 1498, Points: 28
?: Width 1103, Height: 1456, Points: 8
?: Width 1049, Height: 1456, Points: 20
?: Width 1026, Height: 1456, Points: 15
?: Width 1151, Height: 1456, Points: 8
?: Width 1210, Height: 1456, Points: 9
?: Width 1273, Height: 1456, Points: 36
?: Width 1172, Height: 1456, Points: 12
?: Width 1220, Height: 1456, Points: 26
?: Width 1120, Height: 1477, Points: 36
?: Width 689, Height: 1805, Points: 12
?: Width 1210, Height: 1804, Points: 17
?: Width 1054, Height: 1571, Points: 48
?: Width 903, Height: 1568, Points: 47
?: Width 870, Height: 1965, Points: 24
?: Width 430, Height: 1551, Points: 20
?: Width 921, Height: 1678, Points: 34
?: Width 1054, Height: 1123, Points: 44
?: Width 946, Height: 1860, Points: 43
?: Width 950, Height: 1499, Points: 12
?: Width 969, Height: 1477, Points: 37
?: Width 903, Height: 1119, Points: 43
?: Width 855, Height: 1880, Points: 34
?: Width 870, Height: 1517, Points: 20
?: Width 927, Height: 1498, Points: 32
?: Width 430, Height: 1102, Points: 16
?: Width 935, Height: 1082, Points: 15
?: Width 1062, Height: 1535, Points: 34
?: Width 857, Height: 1498, Points: 22
?: Width 950, Height: 1082, Points: 10
?: Width 909, Height: 1900, Points: 47
?: Width 969, Height: 1123, Points: 28
?: Width 1149, Height: 1103, Points: 24
?: Width 917, Height: 1518, Points: 31
?: Width 895, Height: 1527, Points: 35
?: Width 1051, Height: 1103, Points: 31
?: Width 907, Height: 1082, Points: 8
?: Width 921, Height: 1103, Points: 22
?: Width 1284, Height: 1560, Points: 34
?: Width 1004, Height: 1534, Points: 38
?: Width 1266, Height: 1556, Points: 30
?: Width 1524, Height: 1103, Points: 41
?: Width 689, Height: 1483, Points: 24
?: Width 921, Height: 1483, Points: 30
?: Width 969, Height: 1571, Points: 32
?: Width 921, Height: 1551, Points: 26
?: Width 1524, Height: 1551, Points: 45
?: Width 1087, Height: 1498, Points: 39
?: Width 1129, Height: 1467, Points: 36
?: Width 1483, Height: 1103, Points: 45
?: Width 897, Height: 1867, Points: 17
?: Width 897, Height: 1805, Points: 20
?: Width 1383, Height: 1506, Points: 24
?: Width 893, Height: 1863, Points: 11
?: Width 1097, Height: 1498, Points: 34
?: Width 1034, Height: 1498, Points: 40
?: Width 198, Height: 1456, Points: 4
?: Width 689, Height: 1805, Points: 12
?: Width 882, Height: 1477, Points: 17
?: Width 2071, Height: 1456, Points: 32
?: Width 1953, Height: 1456, Points: 28
?: Width 1454, Height: 1456, Points: 20
?: Width 1111, Height: 1775, Points: 20
?: Width 1101, Height: 1867, Points: 17
?: Width 1158, Height: 1891, Points: 38
?: Width 1103, Height: 1815, Points: 12
?: Width 1208, Height: 1456, Points: 12
?: Width 1048, Height: 1456, Points: 22
?: Width 1059, Height: 1456, Points: 34
?: Width 893, Height: 1456, Points: 6
?: Width 1464, Height: 1815, Points: 22
?: Width 897, Height: 1456, Points: 12
?: Width 1634, Height: 1456, Points: 22
?: Width 1127, Height: 1498, Points: 43
?: Width 1101, Height: 1456, Points: 12
?: Width 1101, Height: 1870, Points: 28
?: Width 1111, Height: 1456, Points: 15
?: Width 1222, Height: 1456, Points: 16
?: Width 1438, Height: 1456, Points: 16
?: Width 1101, Height: 1456, Points: 12
?: Width 1169, Height: 1498, Points: 28
?: Width 1103, Height: 1456, Points: 8
?: Width 1049, Height: 1456, Points: 20
?: Width 1097, Height: 1498, Points: 30
?: Width 1151, Height: 1456, Points: 8
?: Width 1158, Height: 1477, Points: 22
?: Width 1273, Height: 1456, Points: 36
?: Width 1172, Height: 1456, Points: 12
?: Width 1292, Height: 1815, Points: 12
?: Width 1081, Height: 1456, Points: 20
?: Width 1771, Height: 1456, Points: 12
?: Width 1941, Height: 1816, Points: 16
?: Width 1374, Height: 1456, Points: 22
?: Width 1423, Height: 1456, Points: 24
?: Width 1048, Height: 1456, Points: 20
?: Width 1098, Height: 1498, Points: 34
?: Width 1572, Height: 1498, Points: 36
?: Width 1034, Height: 1456, Points: 23
?: Width 905, Height: 1123, Points: 44
?: Width 969, Height: 1574, Points: 47
?: Width 915, Height: 1082, Points: 33
?: Width 688, Height: 1082, Points: 6
?: Width 1158, Height: 1440, Points: 22
?: Width 897, Height: 1123, Points: 32
?: Width 1420, Height: 1082, Points: 22
?: Width 904, Height: 1119, Points: 43
?: Width 877, Height: 1082, Points: 12
?: Width 877, Height: 1527, Points: 28
?: Width 935, Height: 1082, Points: 15
?: Width 994, Height: 1082, Points: 16
?: Width 1212, Height: 1082, Points: 16
?: Width 876, Height: 1082, Points: 12
?: Width 969, Height: 1123, Points: 28
?: Width 877, Height: 1082, Points: 8
?: Width 922, Height: 1518, Points: 32
?: Width 888, Height: 1123, Points: 30
?: Width 906, Height: 1082, Points: 8
?: Width 974, Height: 1519, Points: 22
?: Width 1629, Height: 1976, Points: 60
?: Width 934, Height: 1082, Points: 12
?: Width 1009, Height: 1436, Points: 12
?: Width 861, Height: 1083, Points: 20
?: Width 1353, Height: 1082, Points: 12
?: Width 1529, Height: 1420, Points: 16
?: Width 1177, Height: 1082, Points: 22
?: Width 1259, Height: 1082, Points: 24
?: Width 871, Height: 1082, Points: 20
?: Width 896, Height: 1123, Points: 34
?: Width 1419, Height: 1123, Points: 34
?: Width 932, Height: 1082, Points: 23
?: Width 897, Height: 1567, Points: 37
?: Width 897, Height: 1505, Points: 40
?: Width 1039, Height: 1997, Points: 42
?: Width 688, Height: 1520, Points: 11
?: Width 888, Height: 1123, Points: 34
?: Width 866, Height: 1123, Points: 40
?: Width 197, Height: 1560, Points: 8
?: Width 689, Height: 1462, Points: 12
?: Width 436, Height: 1997, Points: 20
?: Width 1629, Height: 1082, Points: 32
?: Width 1550, Height: 1082, Points: 28
?: Width 1005, Height: 1560, Points: 28
?: Width 935, Height: 1519, Points: 20
?: Width 877, Height: 1524, Points: 17
?: Width 974, Height: 1964, Points: 38
?: Width 877, Height: 1438, Points: 12
?: Width 1712, Height: 1498, Points: 61
?: Width 1310, Height: 1082, Points: 25
?: Width 1262, Height: 1456, Points: 28
?: Width 1061, Height: 1562, Points: 28
?: Width 1491, Height: 1498, Points: 42
?: Width 1260, Height: 1123, Points: 40
?: Width 1208, Height: 1456, Points: 16
?: Width 1052, Height: 1082, Points: 18
?: Width 1571, Height: 1456, Points: 24
?: Width 1326, Height: 1082, Points: 26
?: Width 1462, Height: 1456, Points: 38
?: Width 1218, Height: 1083, Points: 35
?: Width 1988, Height: 1456, Points: 46
?: Width 1720, Height: 1083, Points: 43
?: Width 858, Height: 2350, Points: 54
?: Width 816, Height: 2006, Points: 54
?: Width 1220, Height: 1456, Points: 26
?: Width 1266, Height: 1556, Points: 30
?: Width 1169, Height: 1498, Points: 48
?: Width 969, Height: 1123, Points: 46
?: Width 1281, Height: 1476, Points: 22
?: Width 992, Height: 1101, Points: 22
?: Width 1281, Height: 1864, Points: 33
?: Width 1040, Height: 1568, Points: 33
?: Width 2283, Height: 1914, Points: 50
?: Width 2067, Height: 1539, Points: 50
?: Width 1169, Height: 1730, Points: 36
?: Width 969, Height: 1324, Points: 36
?: Width 1713, Height: 2050, Points: 81
?: Width 1432, Height: 1701, Points: 81
?: Width 1712, Height: 1818, Points: 73
?: Width 1310, Height: 1461, Points: 37
?: Width 1097, Height: 1860, Points: 26
?: Width 895, Height: 1485, Points: 26
?: Width 1060, Height: 1342, Points: 20
?: Width 719, Height: 344, Points: 8
?: Width 760, Height: 254, Points: 18
?: Width 245, Height: 322, Points: 6
?: Width 245, Height: 322, Points: 6
?: Width 1945, Height: 1771, Points: 128
?: Width 1856, Height: 1891, Points: 41
?: Width 1265, Height: 2167, Points: 34
?: Width 1041, Height: 1824, Points: 34
?: Width 1262, Height: 1456, Points: 28
?: Width 1094, Height: 1082, Points: 28
?: Width 1049, Height: 1456, Points: 24
?: Width 922, Height: 1518, Points: 36
?: Width 897, Height: 1790, Points: 10
?: Width 693, Height: 1397, Points: 8
?: Width 1077, Height: 1456, Points: 14
?: Width 844, Height: 1082, Points: 14
?: Width 977, Height: 1511, Points: 22
?: Width 821, Height: 1339, Points: 22
?: Width 1705, Height: 1815, Points: 26
?: Width 1456, Height: 1441, Points: 26
?: Width 1127, Height: 1899, Points: 63
?: Width 904, Height: 1521, Points: 63
?: Width 1175, Height: 1815, Points: 19
?: Width 985, Height: 1441, Points: 19
?: Width 1116, Height: 1456, Points: 21
?: Width 970, Height: 1082, Points: 21
?: Width 1322, Height: 1456, Points: 23
?: Width 1104, Height: 1560, Points: 21
?: Width 1646, Height: 1456, Points: 17
?: Width 1418, Height: 1082, Points: 17
?: Width 1246, Height: 1815, Points: 16
?: Width 1021, Height: 1441, Points: 16
?: Width 1734, Height: 1456, Points: 14
?: Width 1238, Height: 1082, Points: 14
?: Width 1851, Height: 1506, Points: 24
?: Width 1577, Height: 1336, Points: 24
?: Width 1169, Height: 1496, Points: 49
?: Width 969, Height: 1121, Points: 49
?: Width 1097, Height: 1899, Points: 50
?: Width 888, Height: 1524, Points: 50
?: Width 1151, Height: 1815, Points: 12
?: Width 906, Height: 1441, Points: 12
?: Width 1210, Height: 1456, Points: 9
?: Width 950, Height: 1499, Points: 12
?: Width 1210, Height: 1456, Points: 16
?: Width 950, Height: 1499, Points: 18
?: Width 1198, Height: 1815, Points: 16
?: Width 989, Height: 1441, Points: 16
?: Width 1637, Height: 1811, Points: 20
?: Width 1277, Height: 1437, Points: 16
?: Width 1226, Height: 1815, Points: 24
?: Width 1006, Height: 1442, Points: 24
?: Width 1081, Height: 1456, Points: 24
?: Width 861, Height: 1083, Points: 24
?: Width 1081, Height: 1456, Points: 20
?: Width 861, Height: 1083, Points: 20
?: Width 1503, Height: 1498, Points: 40
?: Width 1138, Height: 1123, Points: 39
?: Width 1503, Height: 1907, Points: 60
?: Width 1138, Height: 1534, Points: 59
?: Width 198, Height: 1456, Points: 4
?: Width 1634, Height: 1870, Points: 38
?: Width 1420, Height: 1527, Points: 38
?: Width 1070, Height: 1608, Points: 24
?: Width 901, Height: 1340, Points: 24
?: Width 1386, Height: 1753, Points: 22
?: Width 1158, Height: 1379, Points: 22
?: Width 1101, Height: 1893, Points: 24
?: Width 876, Height: 1519, Points: 24
?: Width 1265, Height: 1753, Points: 18
?: Width 1040, Height: 1379, Points: 18
?: Width 1081, Height: 1815, Points: 24
?: Width 861, Height: 1442, Points: 24
?: Width 1602, Height: 1753, Points: 22
?: Width 1376, Height: 1379, Points: 22
?: Width 198, Height: 1456, Points: 4
?: Width 1208, Height: 1870, Points: 28
?: Width 905, Height: 1569, Points: 60
?: Width 1208, Height: 1805, Points: 20
?: Width 905, Height: 1504, Points: 52
?: Width 1910, Height: 1456, Points: 20
?: Width 1602, Height: 1123, Points: 67
?: Width 897, Height: 1870, Points: 28
?: Width 897, Height: 1570, Points: 48
?: Width 1224, Height: 1498, Points: 32
?: Width 897, Height: 1122, Points: 32
?: Width 1224, Height: 1780, Points: 40
?: Width 897, Height: 1503, Points: 40
?: Width 1634, Height: 1805, Points: 30
?: Width 1420, Height: 1462, Points: 30
?: Width 1127, Height: 1847, Points: 51
?: Width 904, Height: 1501, Points: 51
?: Width 964, Height: 1477, Points: 29
?: Width 964, Height: 1477, Points: 29
?: Width 1101, Height: 1786, Points: 16
?: Width 877, Height: 1444, Points: 16
?: Width 1101, Height: 1805, Points: 20
?: Width 877, Height: 1462, Points: 20
?: Width 1169, Height: 1847, Points: 36
?: Width 969, Height: 1504, Points: 36
?: Width 1169, Height: 1498, Points: 48
?: Width 969, Height: 1123, Points: 46
?: Width 1169, Height: 1821, Points: 56
?: Width 969, Height: 1532, Points: 54
?: Width 1098, Height: 1847, Points: 42
?: Width 896, Height: 1504, Points: 42
?: Width 1158, Height: 1807, Points: 26
?: Width 974, Height: 1881, Points: 26
?: Width 1158, Height: 1826, Points: 30
?: Width 974, Height: 1899, Points: 30
?: Width 1158, Height: 1888, Points: 33
?: Width 994, Height: 1961, Points: 33
?: Width 1081, Height: 1805, Points: 28
?: Width 861, Height: 1462, Points: 28
?: Width 893, Height: 1815, Points: 10
?: Width 688, Height: 1441, Points: 10
?: Width 1423, Height: 1805, Points: 32
?: Width 1259, Height: 1462, Points: 32
?: Width 1085, Height: 1893, Points: 26
?: Width 864, Height: 1519, Points: 26
?: Width 1249, Height: 1893, Points: 28
?: Width 1040, Height: 1519, Points: 28
?: Width 1172, Height: 1456, Points: 18
?: Width 934, Height: 1082, Points: 18
?: Width 1047, Height: 1456, Points: 20
?: Width 915, Height: 1581, Points: 32
?: Width 1555, Height: 1456, Points: 34
?: Width 1554, Height: 1583, Points: 52
?: Width 1437, Height: 1480, Points: 45
?: Width 1210, Height: 1111, Points: 46
?: Width 897, Height: 1716, Points: 40
?: Width 838, Height: 1362, Points: 40
?: Width 1840, Height: 1480, Points: 34
?: Width 1536, Height: 1106, Points: 34
?: Width 1749, Height: 1480, Points: 30
?: Width 1478, Height: 1106, Points: 30
?: Width 1065, Height: 1498, Points: 34
?: Width 868, Height: 1123, Points: 34
?: Width 1321, Height: 1480, Points: 26
?: Width 1143, Height: 1106, Points: 26
?: Width 1126, Height: 1498, Points: 43
?: Width 903, Height: 1119, Points: 43
?: Width 1418, Height: 1893, Points: 32
?: Width 1190, Height: 1519, Points: 32
?: Width 1208, Height: 1835, Points: 36
?: Width 905, Height: 1481, Points: 68
?: Width 1438, Height: 1863, Points: 21
?: Width 1504, Height: 1541, Points: 41
?: Width 1690, Height: 1867, Points: 27
?: Width 1455, Height: 1524, Points: 27
?: Width 1690, Height: 1863, Points: 27
?: Width 1455, Height: 1520, Points: 27
?: Width 1690, Height: 1805, Points: 30
?: Width 1455, Height: 1462, Points: 30
?: Width 1208, Height: 1793, Points: 16
?: Width 905, Height: 1439, Points: 48
?: Width 1208, Height: 1992, Points: 28
?: Width 905, Height: 1691, Points: 60
?: Width 1248, Height: 2033, Points: 25
?: Width 1092, Height: 1732, Points: 57
?: Width 1251, Height: 2016, Points: 25
?: Width 1104, Height: 1715, Points: 57
?: Width 1208, Height: 2053, Points: 37
?: Width 1037, Height: 1753, Points: 69
?: Width 1208, Height: 2098, Points: 41
?: Width 905, Height: 1798, Points: 73
?: Width 1208, Height: 2201, Points: 25
?: Width 905, Height: 1879, Points: 57
?: Width 1208, Height: 2015, Points: 33
?: Width 905, Height: 1714, Points: 65
?: Width 1208, Height: 2082, Points: 33
?: Width 905, Height: 1781, Points: 65
?: Width 1208, Height: 2163, Points: 45
?: Width 905, Height: 1862, Points: 77
?: Width 1208, Height: 2085, Points: 48
?: Width 905, Height: 1784, Points: 80
?: Width 1208, Height: 2207, Points: 32
?: Width 905, Height: 1885, Points: 64
?: Width 897, Height: 1783, Points: 16
?: Width 897, Height: 1439, Points: 36
?: Width 897, Height: 1992, Points: 28
?: Width 897, Height: 1692, Points: 48
?: Width 897, Height: 1876, Points: 32
?: Width 897, Height: 1576, Points: 52
?: Width 1092, Height: 2033, Points: 25
?: Width 1085, Height: 1733, Points: 45
?: Width 1096, Height: 2016, Points: 25
?: Width 1103, Height: 1716, Points: 45
?: Width 1037, Height: 2053, Points: 37
?: Width 1030, Height: 1754, Points: 57
?: Width 897, Height: 2098, Points: 41
?: Width 897, Height: 1799, Points: 61
?: Width 897, Height: 2191, Points: 25
?: Width 897, Height: 1880, Points: 45
?: Width 325, Height: 1992, Points: 20
?: Width 325, Height: 1650, Points: 20
?: Width 218, Height: 1784, Points: 8
?: Width 218, Height: 1887, Points: 12
?: Width 1169, Height: 1822, Points: 32
?: Width 969, Height: 1448, Points: 32
?: Width 1169, Height: 2034, Points: 44
?: Width 969, Height: 1691, Points: 44
?: Width 1228, Height: 2075, Points: 41
?: Width 1128, Height: 1732, Points: 41
?: Width 1232, Height: 2058, Points: 41
?: Width 1132, Height: 1715, Points: 41
?: Width 1173, Height: 2095, Points: 53
?: Width 1073, Height: 1753, Points: 53
?: Width 1169, Height: 2140, Points: 57
?: Width 969, Height: 1798, Points: 57
?: Width 1169, Height: 2230, Points: 41
?: Width 969, Height: 1888, Points: 41
?: Width 1427, Height: 1866, Points: 43
?: Width 1159, Height: 1562, Points: 43
?: Width 1427, Height: 1870, Points: 43
?: Width 1159, Height: 1566, Points: 43
?: Width 1427, Height: 1995, Points: 54
?: Width 1159, Height: 1691, Points: 54
?: Width 1427, Height: 1879, Points: 58
?: Width 969, Height: 1575, Points: 48
?: Width 1427, Height: 1990, Points: 42
?: Width 1159, Height: 1569, Points: 42
?: Width 1097, Height: 1801, Points: 22
?: Width 881, Height: 1419, Points: 24
?: Width 1097, Height: 2013, Points: 34
?: Width 881, Height: 1671, Points: 36
?: Width 1476, Height: 1884, Points: 33
?: Width 1247, Height: 1541, Points: 35
?: Width 1476, Height: 1888, Points: 33
?: Width 1247, Height: 1545, Points: 35
?: Width 1476, Height: 2013, Points: 44
?: Width 1247, Height: 1671, Points: 46
?: Width 1476, Height: 1897, Points: 48
?: Width 1247, Height: 1554, Points: 50
?: Width 1476, Height: 1936, Points: 32
?: Width 1247, Height: 1564, Points: 34
?: Width 1210, Height: 1866, Points: 14
?: Width 974, Height: 1961, Points: 27
?: Width 1210, Height: 1783, Points: 13
?: Width 974, Height: 1576, Points: 26
?: Width 1210, Height: 1991, Points: 25
?: Width 974, Height: 2087, Points: 38
?: Width 1210, Height: 1875, Points: 29
?: Width 974, Height: 1970, Points: 42
?: Width 1501, Height: 1704, Points: 40
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0
?: Width 1085, Height: 155, Points: 4
?: Width 1336, Height: 155, Points: 4
?: Width 1336, Height: 155, Points: 4
?: Width 923, Height: 409, Points: 8
?: Width 198, Height: 539, Points: 6
?: Width 198, Height: 539, Points: 6
?: Width 198, Height: 504, Points: 6
?: Width 198, Height: 539, Points: 6
?: Width 526, Height: 539, Points: 12
?: Width 534, Height: 539, Points: 12
?: Width 506, Height: 519, Points: 12
?: Width 990, Height: 1456, Points: 12
?: Width 989, Height: 1872, Points: 20
?: Width 414, Height: 458, Points: 14
?: Width 640, Height: 202, Points: 8
?: Width 1060, Height: 202, Points: 12
?: Width 1813, Height: 1498, Points: 86
?: Width 198, Height: 505, Points: 6
?: Width 507, Height: 520, Points: 11
?: Width 443, Height: 797, Points: 7
?: Width 437, Height: 760, Points: 14
?: Width 737, Height: 1456, Points: 16
?: Width 820, Height: 1205, Points: 4
?: Width 780, Height: 917, Points: 16
?: Width 640, Height: 815, Points: 20
?: Width 1066, Height: 1456, Points: 14
?: Width 1041, Height: 1477, Points: 41
?: Width 1388, Height: 1477, Points: 44
?: Width 1130, Height: 1837, Points: 40
?: Width 902, Height: 1498, Points: 43
?: Width 1287, Height: 1498, Points: 62
?: Width 777, Height: 1496, Points: 41
?: Width 1956, Height: 1473, Points: 44
?: Width 1017, Height: 537, Points: 24
?: Width 1019, Height: 1122, Points: 31
?: Width 1511, Height: 1469, Points: 58
?: Width 1659, Height: 1483, Points: 95
?: Width 1706, Height: 1466, Points: 84
?: Width 1543, Height: 1466, Points: 65
?: Width 1000, Height: 1538, Points: 34
?: Width 1100, Height: 1667, Points: 8
?: Width 1128, Height: 1725, Points: 15
?: Width 835, Height: 155, Points: 4
?: Width 1134, Height: 1456, Points: 12
?: Width 1875, Height: 1123, Points: 54
?: Width 727, Height: 2018, Points: 29
?: Width 943, Height: 752, Points: 52
?: Width 834, Height: 1084, Points: 20
?: Width 840, Height: 1093, Points: 14
?: Width 856, Height: 1096, Points: 14
?: Width 950, Height: 1456, Points: 16
?: Width 219, Height: 377, Points: 6
?: Width 953, Height: 1581, Points: 28
?: Width 974, Height: 1581, Points: 28
?: Width 1599, Height: 1581, Points: 52
?: Width 1648, Height: 1581, Points: 52
?: Width 3376, Height: 2718, Points: 0
?: Width 1939, Height: 1852, Points: 151
?: Width 1915, Height: 2701, Points: 43
?: Width 948, Height: 1622, Points: 56
?: Width 1003, Height: 1559, Points: 27
?: Width 735, Height: 536, Points: 29
?: Width 963, Height: 493, Points: 25
?: Width 1016, Height: 436, Points: 13
?: Width 1017, Height: 436, Points: 13
?: Width 1123, Height: 1165, Points: 12
?: Width 640, Height: 515, Points: 21
?: Width 218, Height: 395, Points: 6
?: Width 884, Height: 1165, Points: 34
?: Width 950, Height: 1198, Points: 30
?: Width 920, Height: 1165, Points: 20
?: Width 815, Height: 1165, Points: 12
?: Width 817, Height: 1165, Points: 10
?: Width 987, Height: 1198, Points: 33
?: Width 961, Height: 1165, Points: 12
?: Width 196, Height: 1165, Points: 4
?: Width 823, Height: 1182, Points: 17
?: Width 936, Height: 1165, Points: 15
?: Width 722, Height: 1165, Points: 6
?: Width 1212, Height: 1165, Points: 16
?: Width 641, Height: 155, Points: 4
?: Width 989, Height: 1165, Points: 12
?: Width 1003, Height: 1198, Points: 28
?: Width 948, Height: 1498, Points: 28
?: Width 2085, Height: 1198, Points: 80
?: Width 906, Height: 1542, Points: 19
?: Width 906, Height: 1482, Points: 14
?: Width 906, Height: 1540, Points: 15
?: Width 1047, Height: 1481, Points: 17
?: Width 1047, Height: 1540, Points: 18
?: Width 1409, Height: 1541, Points: 27
?: Width 1003, Height: 1597, Points: 38
?: Width 1003, Height: 1562, Points: 29
?: Width 1003, Height: 1598, Points: 42
?: Width 1003, Height: 1565, Points: 34
?: Width 1003, Height: 1481, Points: 22
?: Width 1003, Height: 1571, Points: 38
?: Width 906, Height: 1541, Points: 17
?: Width 906, Height: 1643, Points: 14
?: Width 948, Height: 1575, Points: 49
?: Width 846, Height: 1182, Points: 32
?: Width 948, Height: 1574, Points: 49
?: Width 948, Height: 1573, Points: 45
?: Width 915, Height: 1542, Points: 45
?: Width 915, Height: 1643, Points: 42
?: Width 915, Height: 1540, Points: 41
?: Width 1003, Height: 1577, Points: 39
?: Width 1003, Height: 1580, Points: 44
?: Width 1003, Height: 1496, Points: 32
?: Width 989, Height: 1542, Points: 21
?: Width 989, Height: 1643, Points: 18
?: Width 989, Height: 1540, Points: 17
?: Width 722, Height: 1165, Points: 10
?: Width 722, Height: 1166, Points: 12
?: Width 722, Height: 1643, Points: 12
?: Width 769, Height: 1491, Points: 11
?: Width 936, Height: 1645, Points: 21
?: Width 1026, Height: 1549, Points: 26
?: Width 218, Height: 1482, Points: 8
?: Width 399, Height: 1597, Points: 24
?: Width 599, Height: 1547, Points: 20
?: Width 721, Height: 1463, Points: 8
?: Width 694, Height: 1553, Points: 24
?: Width 961, Height: 1541, Points: 21
?: Width 987, Height: 1673, Points: 39
?: Width 987, Height: 1515, Points: 37
?: Width 987, Height: 1580, Points: 49
?: Width 987, Height: 1574, Points: 42
?: Width 815, Height: 1542, Points: 21
?: Width 815, Height: 1597, Points: 32
?: Width 815, Height: 1482, Points: 16
?: Width 815, Height: 1547, Points: 28
?: Width 815, Height: 1463, Points: 16
?: Width 920, Height: 1542, Points: 29
?: Width 950, Height: 1575, Points: 39
?: Width 950, Height: 1515, Points: 34
?: Width 950, Height: 1574, Points: 39
?: Width 950, Height: 1573, Points: 35
?: Width 1174, Height: 1597, Points: 32
?: Width 1123, Height: 1547, Points: 28
?: Width 1123, Height: 1463, Points: 16
?: Width 1047, Height: 1539, Points: 14
?: Width 1003, Height: 1500, Points: 26
?: Width 607, Height: 428, Points: 21
?: Width 1003, Height: 1558, Points: 23
?: Width 1003, Height: 1562, Points: 23
?: Width 1003, Height: 1515, Points: 36
?: Width 1003, Height: 1586, Points: 48
?: Width 1003, Height: 1574, Points: 37
?: Width 1003, Height: 1573, Points: 33
?: Width 1003, Height: 1577, Points: 33
?: Width 989, Height: 1553, Points: 32
?: Width 689, Height: 1482, Points: 12
?: Width 614, Height: 1541, Points: 13
?: Width 424, Height: 1540, Points: 9
?: Width 423, Height: 1544, Points: 9
?: Width 815, Height: 1482, Points: 20
?: Width 815, Height: 1541, Points: 21
?: Width 815, Height: 1540, Points: 17
?: Width 815, Height: 1544, Points: 17
?: Width 950, Height: 1622, Points: 46
?: Width 1123, Height: 1762, Points: 41
?: Width 1123, Height: 1580, Points: 36
?: Width 1123, Height: 1482, Points: 20
?: Width 1123, Height: 1553, Points: 32
?: Width 1123, Height: 1541, Points: 21
?: Width 1123, Height: 1540, Points: 17
?: Width 1123, Height: 1544, Points: 17
?: Width 906, Height: 1165, Points: 12
?: Width 1084, Height: 1165, Points: 24
?: Width 1084, Height: 1165, Points: 24
?: Width 1066, Height: 1299, Points: 34
?: Width 915, Height: 1165, Points: 36
?: Width 599, Height: 268, Points: 16
?: Width 585, Height: 586, Points: 33
?: Width 594, Height: 505, Points: 36
?: Width 197, Height: 516, Points: 4
?: Width 438, Height: 480, Points: 20
?: Width 436, Height: 591, Points: 16
?: Width 810, Height: 930, Points: 16
?: Width 484, Height: 1477, Points: 6
?: Width 916, Height: 1477, Points: 27
?: Width 964, Height: 1498, Points: 43
?: Width 1062, Height: 1456, Points: 16
?: Width 953, Height: 1477, Points: 32
?: Width 968, Height: 1498, Points: 40
?: Width 944, Height: 1456, Points: 13
?: Width 948, Height: 1198, Points: 40
?: Width 906, Height: 1165, Points: 8
?: Width 1003, Height: 1182, Points: 18
?: Width 1116, Height: 1165, Points: 10
?: Width 1409, Height: 1165, Points: 18
?: Width 1035, Height: 1165, Points: 12
?: Width 1047, Height: 1165, Points: 9
?: Width 906, Height: 1165, Points: 10
?: Width 898, Height: 1198, Points: 28
?: Width 373, Height: 1181, Points: 6
?: Width 794, Height: 1181, Points: 27
?: Width 841, Height: 1198, Points: 43
?: Width 974, Height: 1165, Points: 15
?: Width 863, Height: 1198, Points: 40
?: Width 644, Height: 823, Points: 40
?: Width 663, Height: 823, Points: 48
?: Width 612, Height: 342, Points: 12
?: Width 589, Height: 801, Points: 13
?: Width 652, Height: 823, Points: 40
?: Width 644, Height: 812, Points: 32
?: Width 721, Height: 801, Points: 16
?: Width 634, Height: 823, Points: 43
?: Width 601, Height: 812, Points: 27
?: Width 473, Height: 565, Points: 29
?: Width 301, Height: 812, Points: 6
?: Width 678, Height: 823, Points: 28
?: Width 857, Height: 1198, Points: 40
?: Width 881, Height: 1198, Points: 48
?: Width 800, Height: 1165, Points: 13
?: Width 949, Height: 1498, Points: 48
?: Width 964, Height: 1498, Points: 40
?: Width 902, Height: 1165, Points: 20
?: Width 3376, Height: 2718, Points: 0
?: Width 3376, Height: 2718, Points: 0";

        #endregion
    }
}

