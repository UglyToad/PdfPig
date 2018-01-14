namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class LaTexTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "ICML03-081.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("TacklingthePoorAssumptionsofNaiveBayesTextClassiﬁers", page.Text);

                var page2 = document.GetPage(2);

                Assert.Contains("isθc={θc1,θc2,...,θcn},", page2.Text);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(8, document.NumberOfPages);
            }
        }
    }
}