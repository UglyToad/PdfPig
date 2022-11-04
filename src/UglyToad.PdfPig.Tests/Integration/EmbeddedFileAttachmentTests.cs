namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

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
        public void HasCorrectFullPageText()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("paragraph-document.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);

                    Assert.StartsWith("A slightly more complicated solution is necessary is s.Length exceeds Int32.MaxValue. But if\r\nyou need to read a stream that large into memory, you might want to think about a different\r\napproach to your problem.\r\nThis is a text example from Ali Yousefi. Everybody knows this is a test example!\r\n", page.Text);
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

                Assert.Equal(1, files.Count);

                Assert.Equal(20668, files[0].Bytes.Count);
            }
        }
    }
}
