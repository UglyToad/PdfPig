namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class Type0FontTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Type0 Font.pdf");
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

                Assert.True((bool) page.Letters.Any(x => x.GlyphRectangle.Width != 0));
                Assert.True((bool) page.Letters.Any(x => x.GlyphRectangle.Height != 0));
            }
        }
    }
}