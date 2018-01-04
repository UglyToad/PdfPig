namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class MultiplePageMortalityStatistics
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "Multiple Page - from Mortality Statistics.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(6, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectVersion()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(1.7m, document.Version);
            }
        }
    }
}
