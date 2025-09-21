namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

    public class Type0FontTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Type0 Font.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.True(text?.Contains("Powder River Examiner"));
            }
        }

        [Fact]
        public void HasLetterWidthsAndHeights()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains(page.Letters, x => x.GlyphRectangle.Width != 0);
                Assert.Contains(page.Letters, x => x.GlyphRectangle.Height != 0);
                Assert.Contains(page.Letters, x => x.GlyphRectangleLoose.Width != 0);
                Assert.Contains(page.Letters, x => x.GlyphRectangleLoose.Height != 0);
            }
        }
    }
}