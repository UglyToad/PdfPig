namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class PigProductionHandbookTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Pig Production Handbook.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename(), new ParsingOptions
            {
                UseLenientParsing = false
            }))
            {
                var page = document.GetPage(1);

                Assert.Contains("For the small holders at village level", page.Text);
            }
        }

        [Fact]
        public void CanReadPage9()
        {
            using (var document = PdfDocument.Open(GetFilename(), new ParsingOptions
            {
                UseLenientParsing = false
            }))
            {
                var page = document.GetPage(9);

                Assert.Contains("BreedsNative breeds of pig can be found throughout the country. They are a small body size compared to other exotic and crosses pig types. There name varies from region to region, for example", page.Text);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(86, document.NumberOfPages);
            }
        }
    }
}