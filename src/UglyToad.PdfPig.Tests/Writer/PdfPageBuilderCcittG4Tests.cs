namespace UglyToad.PdfPig.Tests.Writer
{
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Tests.Writer.TestImages;
    using UglyToad.PdfPig.Writer;
    using Xunit;

    public class PdfPageBuilderCcittG4Tests
    {
        [Fact]
        public void FixtureProvidesRawCcittData()
        {
            var fixture = CcittG4TestImage.Load();

            Assert.True(fixture.Width > 0);
            Assert.True(fixture.Height > 0);
            Assert.NotEmpty(fixture.RawCcittData);
            Assert.EndsWith("Images/Files/Tif/TiffCcittG4.tif", fixture.SourcePath.Replace('\\', '/'));
            Assert.True(fixture.BlackIs1);
        }

        [Fact]
        public void InspectorMatchesFixtureMetadata()
        {
            var fixture = CcittG4TestImage.Load();
            var info = TiffFaxInspector.Inspect(fixture.SourcePath);

            Assert.Equal(fixture.Width, info.Width);
            Assert.Equal(fixture.Height, info.Height);
            Assert.True(info.IsCcittG4);
            Assert.True(info.IsBilevel);
            Assert.Equal(fixture.BlackIs1, info.BlackIs1);
            Assert.Null(info.Diagnostic);
        }

        [Fact]
        public void AddCcittG4AddsFaxImage()
        {
            var fixture = CcittG4TestImage.Load();

            byte[] pdfBytes;
            using (var documentBuilder = new PdfDocumentBuilder())
            {
                var page = documentBuilder.AddPage(fixture.Width, fixture.Height);

                page.AddCcittG4(fixture.RawCcittData, fixture.Width, fixture.Height, blackIs1: fixture.BlackIs1);

                pdfBytes = documentBuilder.Build();
            }

            File.WriteAllBytes(@"PdfPageBuilderTests_CanAddTifCcittG4.pdf", pdfBytes);

            using (var document = PdfDocument.Open(pdfBytes))
            {
                var page = document.GetPage(1);
                var image = Assert.Single(page.GetImages());

                Assert.Equal(fixture.Width, image.WidthInSamples);
                Assert.Equal(fixture.Height, image.HeightInSamples);
                Assert.Equal(1, image.BitsPerComponent);
            }
        }
    }
}