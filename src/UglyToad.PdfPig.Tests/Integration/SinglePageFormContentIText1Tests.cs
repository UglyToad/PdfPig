namespace UglyToad.PdfPig.Tests.Integration
{
    using System.IO;
    using Content;
    using Xunit;

    public class SinglePageFormContentIText1Tests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Form Content - from itext 1_1.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.A4, page.Size);
            }
        }

        [Fact]
        public void DoesNotExtractText()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Empty(page.Letters);
            }
        }
    }
}
