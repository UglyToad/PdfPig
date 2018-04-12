namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class PigReproductionPowerpointTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Pig Reproduction Powerpoint.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("Pigs per sow per year: 18 to 27", page.Text);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(35, document.NumberOfPages);
            }
        }
    }
}