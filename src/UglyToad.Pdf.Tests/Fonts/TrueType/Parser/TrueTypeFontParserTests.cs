namespace UglyToad.Pdf.Tests.Fonts.TrueType.Parser
{
    using System;
    using System.IO;
    using System.Linq;
    using IO;
    using Pdf.Fonts.TrueType;
    using Pdf.Fonts.TrueType.Parser;
    using Pdf.Fonts.TrueType.Tables;
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

            Assert.Equal(-980, font.HeaderTable.XMin);
            Assert.Equal(-555, font.HeaderTable.YMin);

            Assert.Equal(2396, font.HeaderTable.XMax);
            Assert.Equal(2163, font.HeaderTable.YMax);

            Assert.Equal(HeaderTable.HeaderMacStyle.None, font.HeaderTable.MacStyle);
            Assert.Equal(9, font.HeaderTable.LowestRecommendedPpem);

            Assert.Equal(HeaderTable.FontDirection.StronglyLeftToRightWithNeutrals, font.HeaderTable.FontDirectionHint);

            Assert.Equal(0, font.HeaderTable.IndexToLocFormat);
            Assert.Equal(0, font.HeaderTable.GlyphDataFormat);
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
