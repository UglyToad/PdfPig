namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
    using UglyToad.PdfPig.Fonts.SystemFonts;

    public class HorizontalReadingOrderDetectorTests
    {
        private HorizontalReadingOrderDetector horizontalOrder = new HorizontalReadingOrderDetector();

        [Fact]
        public void CanOrderWordsFromDocument()
        {
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("2559 words.pdf")))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();
                var noSpacesWords = words.Where(x => !string.IsNullOrEmpty(x.Text.Trim()));
                var wordsForTest = noSpacesWords.Take(10).ToList();
                var disOrderedWords = wordsForTest.OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(disOrderedWords).ToList();

                Assert.Equal("Lorem", result[0].Text);
                Assert.Equal("ipsum", result[1].Text);
                Assert.Equal("dolor", result[2].Text);
                Assert.Equal("sit", result[3].Text);
                Assert.Equal("amet,", result[4].Text);
                Assert.Equal("consectetur", result[5].Text);
                Assert.Equal("do", result[9].Text);
            }
        }


        [Fact]
        public void CanOrderLettersFromDocument()
        {
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("2559 words.pdf")))
            {
                var page = document.GetPage(1);
                var disOrderedLetters = page.Letters.Take(6).OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(disOrderedLetters).ToList();

                Assert.Equal("L", result[0].Text);
                Assert.Equal("o", result[1].Text);
                Assert.Equal("r", result[2].Text);
                Assert.Equal("e", result[3].Text);
                Assert.Equal("m", result[4].Text);
                Assert.Equal(" ", result[5].Text);
            }
        }

        [Fact]
        public void CanOrderWordsAt270RotationFromDocument()
        {
            // The 'TimesNewRomanPSMT' font is used by this particular document. Thus, results cannot be trusted on
            // platforms where this font isn't generally available (e.g. OSX, Linux, etc.), so we skip it!
            var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");
            Skip.If(font == null, "Skipped because the font TimesNewRomanPSMT could not be found in the execution environment.");
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("90 180 270 rotated.pdf")))
            {
                var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { LineSeparator = " " };
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = new DocstrumBoundingBoxes(options).GetBlocks(words);
                var orderedBlocks = blocks.OrderBy(b => b.BoundingBox.BottomLeft.X)
                                            .ThenByDescending(b => b.BoundingBox.BottomLeft.Y).ToList();


                var block90 = orderedBlocks[0];
                var wordsForTest = block90.TextLines[0].Words.OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(wordsForTest).ToList();


                Assert.Equal("Morbi", result[0].Text);
                Assert.Equal("euismod", result[1].Text);
                Assert.Equal("mattis", result[2].Text);
                Assert.Equal("libero,", result[3].Text);
                Assert.Equal("nec", result[4].Text);
                Assert.Equal("porta", result[5].Text);
            }
        }

        [Fact]
        public void CanOrderWordsAt90RotationFromDocument()
        {
            // The 'TimesNewRomanPSMT' font is used by this particular document. Thus, results cannot be trusted on
            // platforms where this font isn't generally available (e.g. OSX, Linux, etc.), so we skip it!
            var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");
            Skip.If(font == null, "Skipped because the font TimesNewRomanPSMT could not be found in the execution environment.");
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("90 180 270 rotated.pdf")))
            {
                var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { LineSeparator = " " };
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = new DocstrumBoundingBoxes(options).GetBlocks(words);
                var orderedBlocks = blocks.OrderBy(b => b.BoundingBox.BottomLeft.X)
                                            .ThenByDescending(b => b.BoundingBox.BottomLeft.Y).ToList();


                var block270 = orderedBlocks[1];
                var wordsForTest = block270.TextLines[0].Words.OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(wordsForTest).ToList();


                Assert.Equal("Lorem", result[0].Text);
                Assert.Equal("ipsum", result[1].Text);
                Assert.Equal("dolor", result[2].Text);
            }
        }

        [Fact]
        public void CanOrderWordsAt180RotationFromDocument()
        {
            // The 'TimesNewRomanPSMT' font is used by this particular document. Thus, results cannot be trusted on
            // platforms where this font isn't generally available (e.g. OSX, Linux, etc.), so we skip it!
            var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");
            Skip.If(font == null, "Skipped because the font TimesNewRomanPSMT could not be found in the execution environment.");
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("90 180 270 rotated.pdf")))
            {
                var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { LineSeparator = " " };
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = new DocstrumBoundingBoxes(options).GetBlocks(words);
                var orderedBlocks = blocks.OrderBy(b => b.BoundingBox.BottomLeft.X)
                                            .ThenByDescending(b => b.BoundingBox.BottomLeft.Y).ToList();


                var block180 = orderedBlocks[2];
                var wordsForTest = block180.TextLines[0].Words.OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(wordsForTest).ToList();


                Assert.Equal("Cras", result[0].Text);
                Assert.Equal("gravida", result[1].Text);
                Assert.Equal("vel", result[2].Text);
            }
        }

        [Fact]
        public void CanOrderWordsAtRandomRotationFromDocument()
        {
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("complex rotated.pdf")))
            {
                var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { LineSeparator = " " };
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = new DocstrumBoundingBoxes(options).GetBlocks(words);
                var orderedBlocks = blocks.OrderBy(b => b.BoundingBox.BottomLeft.X)
                                            .ThenByDescending(b => b.BoundingBox.BottomLeft.Y).ToList();


                var block = orderedBlocks[0];
                var wordsForTest = block.TextLines[0].Words.OrderBy(x => x.Text.GetHashCode()).ToList();

                // Act
                var result = horizontalOrder.Get(wordsForTest).ToList();


                Assert.Equal("Lorem", result[0].Text);
                Assert.Equal("ipsum", result[1].Text);
                Assert.Equal("dolor", result[2].Text);
            }
        }

    }
}
