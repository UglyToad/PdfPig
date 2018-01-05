namespace UglyToad.Pdf.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class MultiplePageMortalityStatisticsTests
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

        [Fact]
        public void GetsFirstPageContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("Mortality Statistics: Metadata", page.Text);
                Assert.Contains("Notification to the registrar by the coroner that he does not consider it necessary to hold an inquest – no post-mortem held (Form 100A – salmon pink)", page.Text);
                Assert.Contains("Presumption of death certificate", page.Text);
            }
        }
    }
}
