using UglyToad.PdfPig.Tests.Integration;
using UglyToad.PdfPig.Writer;
using System.IO;
using Xunit;

namespace UglyToad.PdfPig.Tests.Writer
{
    public class PdfTextRemoverTests
    {
        [Theory]
        [InlineData("Two Page Text Only - from libre office.pdf")]
        [InlineData("cat-genetics.pdf")]
        [InlineData("Motor Insurance claim form.pdf")]
        [InlineData("Single Page Images - from libre office.pdf")]
        public void TextRemoverRemovesText(string file)
        {
            var filePath = IntegrationHelpers.GetDocumentPath(file);
            using (var document = PdfDocument.Open(filePath))
            {
                var withoutText = PdfTextRemover.RemoveText(filePath);
                File.WriteAllBytes(@"C:\temp\_tx.pdf", withoutText);
                using (var documentWithoutText = PdfDocument.Open(withoutText))
                {
                    Assert.Equal(document.NumberOfPages, documentWithoutText.NumberOfPages);
                    for (var i = 1; i <= documentWithoutText.NumberOfPages; i++)
                    {
                        Assert.NotEqual(document.GetPage(i).Text, string.Empty);
                        Assert.Equal(documentWithoutText.GetPage(i).Text, string.Empty);
                    }

                }
            }
        }
    }
}
