namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class Type1FontSimpleTests
    {
        [Fact]
        public void Issue807()
        {
            var file = IntegrationHelpers.GetDocumentPath("Diacritics_export.pdf");

            using (var document = PdfDocument.Open(file))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToArray();

                Assert.Equal(3, words.Length);
                Assert.Equal("Espinosa", words[0].Text);
                Assert.Equal("Spínola", words[1].Text);
                Assert.Equal("Moraña,", words[2].Text);
            }
        }
    }
}
