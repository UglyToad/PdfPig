namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using Xunit;

    public class FontFileTests
    {
        [Fact]
        public void GetWordsShouldNotFailOnBrokenFontFile()
        {
            var pdf = IntegrationHelpers.GetDocumentPath("pdfpig_error_data.Seek.position._page");
            PdfDocument.Open(pdf).GetPages().First().GetWords();
        }
    }
}