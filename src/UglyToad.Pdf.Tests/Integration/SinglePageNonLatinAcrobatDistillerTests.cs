namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class SinglePageNonLatinAcrobatDistillerTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Single Page Non Latin - from acrobat distiller.pdf");
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

                // For some reason the C# string reverses these characters but they are extracted correctly.
                // TODO: Need someone who can read these to check them
                Assert.Equal("Hello ﺪﻤﺤﻣ World. ", text);
            }
        }
    }
}
