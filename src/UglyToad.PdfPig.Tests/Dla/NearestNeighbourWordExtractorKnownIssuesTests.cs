namespace UglyToad.PdfPig.Tests.Dla
{
    using static NearestNeighbourWordExtractorQualitativeTests;

    /// <summary>
    /// Known issues of the <see cref="UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor.NearestNeighbourWordExtractor"/>.
    /// The tests assert the expected words, and are skipped until the issue is fixed.
    /// </summary>
    public class NearestNeighbourWordExtractorKnownIssuesTests
    {
        [Fact(Skip = "Known issue: zero width spaces (U+200B) are not white spaces for .NET, so they are part of the words (e.g. 'This\\u200B') and form words on their own.")]
        public void ZeroWidthSpacesAreNotPartOfWords()
        {
            // Each word is surrounded by zero width spaces, e.g. "This\u200B \u200Bis\u200B \u200Bthe"
            var words = GetWords("Single Page Simple - from google drive", 1)
                .Select(w => w.Text)
                .Where(t => t != "\u200B");

            Assert.Equal(new[]
            {
                "This", "is", "the", "document", "title",
                "There", "is", "some", "lede", "text", "here",
                "And", "then", "another", "line", "of", "text."
            }, words, StringComparer.Ordinal);
        }

        [Fact(Skip = "Known issue: words without a space glyph between them are merged when the gap is below the maximum distance (20% of the font size), e.g. 'LesenSie'.")]
        public void WordsWithoutSpaceGlyphAreNotMerged()
        {
            // The first line has no space glyph between 'Lesen' and 'Sie', with a gap of 1.48 for a font size of 8
            var words = GetWords("11194059_2017-11_de_s", 1);

            AssertContainsWords(words, "Lesen Sie die gesamte Packungsbeilage sorgfältig durch, bevor Sie mit der Anwendung dieses Arzneimittels beginnen,");
        }

        [Fact(Skip = "Known issue: right-to-left text is not reordered, the letters are in the order they are drawn (left to right), e.g. the Arabic word is reversed.")]
        public void RightToLeftWordIsInLogicalOrder()
        {
            // 'Muhammad' in Arabic presentation forms, drawn from left to right: U+FEAA U+FEE4 U+FEA4 U+FEE3
            var words = GetWords("Single Page Non Latin - from acrobat distiller", 1);

            AssertContainsWords(words, "Hello ﻣﺤﻤﺪ World.");
        }
    }
}
