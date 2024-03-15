namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

    public class JudgementDocumentTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Judgement Document.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(13, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageContents()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("Royal Courts of Justice, Rolls Building Fetter Lane, London, EC4A 1NL", page.Text);
                
                page = document.GetPage(2);

                Assert.Contains("The reference to BAR is to another trade organisation of which CMUK was", page.Text);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var pages = Enumerable.Range(1, 13)
                    .Select(x => document.GetPage(x))
                    .ToList();

                Assert.All(pages, x => Assert.Equal(PageSize.A4, x.Size));
            }
        }
    }
}