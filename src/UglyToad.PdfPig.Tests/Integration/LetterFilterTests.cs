namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Fonts.SystemFonts;
    using System.Linq;

    public class LetterFilterTests
    {
        [SkippableFact]
        public void CanFilterClippedLetters()
        {
            var one = IntegrationHelpers.GetDocumentPath("ClipPathLetterFilter-Test1.pdf");

            // The 'TimesNewRomanPSMT' font is used by this particular document. Thus, results cannot be trusted on
            // platforms where this font isn't generally available (e.g. OSX, Linux, etc.), so we skip it!
            var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");
            var font1 = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPS-BoldMT");
            var font2 = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPS-ItalicMT");
            Skip.If(font is null || font1 is null || font2 is null, "Skipped because the font TimesNewRomanPSMT or a font from TimesNewRoman family could not be found in the execution environment.");

            using (var doc1 = PdfDocument.Open(one, new ParsingOptions { ClipPaths = true }))
            using (var doc2 = PdfDocument.Open(one, new ParsingOptions { ClipPaths = false }))
            {
                var allLetters = doc2.GetPage(5).Letters.Count;
                var filteredLetters = doc1.GetPage(5).Letters.Count;

                Assert.True(filteredLetters < allLetters,
                    "Expected filtered letter count to be lower than non-filtered"); // Filtered: 3158 letters, Non-filtered: 3184 letters
            }
        }

        [Fact]
        public void CanFilterClippedLetters_CheckBleedInSpecificWord()
        {
            var one = IntegrationHelpers.GetDocumentPath("ClipPathLetterFilter-Test2.pdf");

            using (var doc1 = PdfDocument.Open(one, new ParsingOptions { ClipPaths = true }))
            using (var doc2 = PdfDocument.Open(one, new ParsingOptions { ClipPaths = false }))
            {
                var allWords = doc2.GetPage(1).GetWords().ToList();
                var filteredWords = doc1.GetPage(1).GetWords().ToList();

                // The table has hidden columns at the left end. Letters from these columns get merged in words 
                // which is incorrect. Filtering letters based on clip path should fix that...
                const string wordToSearchAfterWhichTheActualTableStarts = "ARISER";

                var indexOfCheckedWordInAllWords = allWords.FindIndex(x => x.Text.Equals(wordToSearchAfterWhichTheActualTableStarts)) + 1;
                Assert.True(allWords[indexOfCheckedWordInAllWords].Text == "MLIA0U01CP00O0I3N6G2");

                var indexOfCheckedWordInFilteredWords = filteredWords.FindIndex(x => x.Text.Equals(wordToSearchAfterWhichTheActualTableStarts)) + 1;
                Assert.True(filteredWords[indexOfCheckedWordInFilteredWords].Text == "ACOGUT");
            }
        }
    }
}
