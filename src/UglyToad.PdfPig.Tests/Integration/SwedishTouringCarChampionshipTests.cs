namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;
    using Images.Png;
    using Xunit;

    public class SwedishTouringCarChampionshipTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("2006_Swedish_Touring_Car_Championship.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(4, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectVersion()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(1.4m, document.Version);
            }
        }

        [Fact]
        public void GetsFirstPageContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("A privateers championship named Caran Cup was created for drivers using cars constructed in 2003 or earlier", page.Text);

                Assert.Equal(PageSize.A4, page.Size);
            }
        }

        [Fact]
        public void GetsSwedishCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(2);

                Assert.Contains("Vålerbanen", page.Text);

                page = document.GetPage(3);

                Assert.Contains("Söderberg", page.Text);
            }
        }

        [Fact]
        public void GetsHyperlinks()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var links = page.GetHyperlinks();

                Assert.Equal(4, links.Count);

                var pageLink = links[0];

                Assert.Equal("Swedish Touring Car Championship", pageLink.Text);
                Assert.Equal("https://en.wikipedia.org/wiki/Swedish_Touring_Car_Championship", pageLink.Uri);

                var year2005 = links[1];

                Assert.Equal("2005", year2005.Text);
                Assert.Equal("https://en.wikipedia.org/wiki/2005_Swedish_Touring_Car_Championship", year2005.Uri);

                var year2007 = links[2];

                Assert.Equal("2007", year2007.Text);
                Assert.Equal("https://en.wikipedia.org/wiki/2007_Swedish_Touring_Car_Championship", year2007.Uri);
                
                var fullLink = links[3];

                Assert.Equal("The 2006 Swedish Touring Car Championship season was the 11th Swedish Touring Car Championship (STCC) season. " +
                             "In total nine racing weekends at six different circuits were held; each", fullLink.Text);
                Assert.Equal("https://en.wikipedia.org/wiki/Swedish_Touring_Car_Championship", fullLink.Uri);
            }
        }

        [Fact]
        public void GetsImagesAsPng()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                foreach (var page in document.GetPages())
                {
                    foreach (var image in page.GetImages())
                    {
                        if (!image.TryGetBytes(out _))
                        {
                            continue;
                        }

                        Assert.True(image.TryGetPng(out var png));

                        var pngActual = Png.Open(png);

                        Assert.NotNull(pngActual);
                    }
                }
            }
        }
    }
}
