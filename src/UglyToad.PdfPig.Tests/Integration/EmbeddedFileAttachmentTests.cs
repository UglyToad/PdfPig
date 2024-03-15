namespace UglyToad.PdfPig.Tests.Integration
{
    public class EmbeddedFileAttachmentTests
    {
        [Fact]
        public void HasCorrectText()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);

                    Assert.StartsWith("This is a test document. It contains a file attachment.", page.Text);
                }
            }
        }

        [Fact]
        public void HasEmbeddedFiles()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf");

            using (var document = PdfDocument.Open(path))
            {
                Assert.True(document.Advanced.TryGetEmbeddedFiles(out var files));

                Assert.Single(files);

                Assert.Equal(20668, files[0].Bytes.Count);
            }
        }
    }
}
