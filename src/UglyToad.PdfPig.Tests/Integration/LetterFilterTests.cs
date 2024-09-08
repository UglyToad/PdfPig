namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class LetterFilterTests
    {
        [Fact]
        public void CanFilterClippedLetters()
        {
            var one = IntegrationHelpers.GetDocumentPath("ClipPathLetterFilter-Test1.pdf");

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
