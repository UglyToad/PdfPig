namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using Xunit;

    public class PlosOneTests
    {
        private static string GetFilename()
        {
            var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

            return Path.Combine(documentFolder, "journal.pone.0196757.pdf");
        }

        [Fact]
        public void CanReadPageOneContent()
        {
            using (var document = PdfDocument.Open(GetFilename().Replace("ICML03-081.pdf", "journal.pone.0196757.pdf")))
            {
                var page = document.GetPage(1);
                var text = page.Text;
                Assert.True(text.Length > 50);
            }
        }
    }
}
