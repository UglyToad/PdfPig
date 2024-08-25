namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class TrueTypeTests
    {
        [Fact]
        public void Issue881()
        {
            var file = IntegrationHelpers.GetDocumentPath("issue_881.pdf");

            using (var document = PdfDocument.Open(file))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToArray();
                Assert.Equal(4, words.Length);
                Assert.Equal("IDNR:", words[0].Text);
                Assert.Equal("4174", words[1].Text);
                Assert.Equal("/", words[2].Text);
                Assert.Equal("06.08.2018", words[3].Text);
            }
        }
    }
}
