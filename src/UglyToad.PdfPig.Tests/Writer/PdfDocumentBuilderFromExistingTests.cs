namespace UglyToad.PdfPig.Tests.Writer
{
    using System.IO;
    using Integration;
    using PdfPig.Writer;
    using Xunit;

    public class PdfDocumentBuilderFromExistingTests
    {
        [Fact]
        public void LoadAndSaveExistingNoModifications()
        {
            var path = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");

            var bytes = File.ReadAllBytes(path);

            var builder = PdfDocumentBuilder.FromPdf(bytes);

            var output = builder.Build();
            
            Assert.NotNull(output);
        }
    }
}
