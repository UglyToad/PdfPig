namespace UglyToad.PdfPig.Tests.Fonts.TrueType
{
    using PdfPig.Core;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Fonts.TrueType.Subsetting;

    public class TrueTypeSubsetterTests
    {
        [Fact]
        public void SubsetOfMoreThan256CharactersKeepsTheGlyphOfEachCharacter()
        {
            var fontBytes = TrueTypeTestHelper.GetFileBytes("Roboto-Regular");
            var font = Parse(fontBytes);

            // Basic Latin, Latin-1 Supplement and Latin Extended-A, without control characters.
            var characters = Enumerable.Range(0x21, 94)
                .Concat(Enumerable.Range(0xA0, 224))
                .Select(x => (char)x)
                .ToArray();

            var subset = Parse(TrueTypeSubsetter.Subset(fontBytes, new TrueTypeSubsetEncoding(characters)));

            for (var i = 0; i < characters.Length; i++)
            {
                // Glyph 0 is .notdef, the characters follow in order.
                var glyphIndex = i + 1;

                Assert.True(font.TryGetPath(characters[i], out var expectedPath));
                Assert.True(subset.TryGetPath(glyphIndex, _ => glyphIndex, out var actualPath));
                Assert.Equal(expectedPath.SelectMany(x => x.Commands), actualPath.SelectMany(x => x.Commands));

                Assert.True(font.TryGetAdvanceWidth(characters[i], out var expectedWidth));
                Assert.True(subset.TryGetAdvanceWidth(glyphIndex, _ => glyphIndex, out var actualWidth));
                Assert.Equal(expectedWidth, actualWidth);
            }
        }

        [Fact]
        public void SubsetKeepsTheMetricsOfGlyphsAfterTheLastHorizontalMetric()
        {
            // A subset of Roboto-Regular with only the digits. They share an advance width, so the hmtx table has full
            // metrics for .notdef and zero only, and left side bearings for the other digits.
            var fontBytes = TrueTypeTestHelper.GetFileBytes("Roboto-Regular-Subset-Digits");
            var font = Parse(fontBytes);

            var characters = "0123456789".ToCharArray();

            var subset = Parse(TrueTypeSubsetter.Subset(fontBytes, new TrueTypeSubsetEncoding(characters)));
            var expectedMetrics = font.TableRegister.HorizontalMetricsTable;
            var actualMetrics = subset.TableRegister.HorizontalMetricsTable;

            for (var i = 0; i < characters.Length; i++)
            {
                var glyphIndex = i + 1;
                var originalGlyphIndex = font.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(characters[i]);

                Assert.True(font.TryGetAdvanceWidth(characters[i], out var expectedWidth));
                Assert.True(subset.TryGetAdvanceWidth(glyphIndex, _ => glyphIndex, out var actualWidth));
                Assert.Equal(expectedWidth, actualWidth);

                var expectedLeftSideBearing = originalGlyphIndex < expectedMetrics.HorizontalMetrics.Count
                    ? expectedMetrics.HorizontalMetrics[originalGlyphIndex].LeftSideBearing
                    : expectedMetrics.AdditionalLeftSideBearings[originalGlyphIndex - expectedMetrics.HorizontalMetrics.Count];
                Assert.Equal(expectedLeftSideBearing, actualMetrics.HorizontalMetrics[glyphIndex].LeftSideBearing);
            }
        }

        private static TrueTypeFont Parse(byte[] fontBytes)
        {
            return TrueTypeFontParser.Parse(new TrueTypeDataBytes(new MemoryInputBytes(fontBytes)));
        }
    }
}