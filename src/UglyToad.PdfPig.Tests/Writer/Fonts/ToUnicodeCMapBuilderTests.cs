namespace UglyToad.PdfPig.Tests.Writer.Fonts
{
    using PdfFonts.Parser;
    using PdfPig.Core;
    using PdfPig.Writer.Fonts;

    public class ToUnicodeCMapBuilderTests
    {
        [Fact]
        public void WritesValidCMap()
        {
            var mappings = new Dictionary<char, byte>
            {
                {'1', 1},
                {'=', 2},
                {'H', 7},
                {'a', 12},
                {'2', 25}
            };

            var cmapStream = ToUnicodeCMapBuilder.ConvertToCMapStream(mappings);

            var str = OtherEncodings.BytesAsLatin1String(cmapStream);

            Assert.NotNull(str);

            var result = new CMapParser().Parse(new ByteArrayInputBytes(cmapStream));

            Assert.Single(result.CodespaceRanges);

            var range = result.CodespaceRanges[0];

            Assert.Equal(1, range.CodeLength);
            Assert.Equal(0, range.StartInt);
            Assert.Equal(byte.MaxValue, range.EndInt);

            Assert.Equal(mappings.Count, result.BaseFontCharacterMap.Count);

            foreach (var keyValuePair in result.BaseFontCharacterMap)
            {
                var match = mappings[keyValuePair.Value[0]];

                Assert.Equal(match, keyValuePair.Key);
            }
        }
    }
}
