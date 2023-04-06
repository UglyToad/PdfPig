namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using Xunit;

    public class ColorSpaceTests
    {
        [Fact]
        public void CanGetAllPagesImages()
        {
            var path = IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (int p = 0; p < document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p + 1);
                    var images = page.GetImages().ToArray();
                    foreach (var image in images)
                    {
                        if (image.TryGetPng(out var png))
                        {

                        }
                    }
                }
            }
        }

        [Fact]
        public void SeparationIccColorSpacesWithForm()
        {
            var path = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");

            using (var document = PdfDocument.Open(path))
            {
                Page page1 = document.GetPage(1);
                var paths1 = page1.ExperimentalAccess.Paths.Where(p => p.IsFilled).ToArray();
                Assert.Equal((0.930496m, 0.111542m, 0.142197m), paths1[0].FillColor.ToRGBValues()); // 'Reflex Red' Separation color space

                Page page2 = document.GetPage(2);
                var words = page2.GetWords(NearestNeighbourWordExtractor.Instance).ToArray();

                var urlWord = words.First(w => w.ToString().Contains("www.extron.com"));
                var firstLetter = urlWord.Letters[0];
                Assert.Equal("w", firstLetter.Value);
                Assert.Equal((0, 0, 1), firstLetter.Color.ToRGBValues()); // Blue

                var paths2 = page2.ExperimentalAccess.Paths;
                var filledPath = paths2.Where(p => p.IsFilled).ToArray();
                var filledRects = filledPath.Where(p => p.Count == 1 && p[0].IsDrawnAsRectangle).ToArray();

                // Colors picked from Acrobat reader
                (decimal r, decimal g, decimal b) lightRed = (0.985m, 0.942m, 0.921m);
                (decimal r, decimal g, decimal b) lightRed2 = (1m, 0.95m, 0.95m);
                (decimal r, decimal g, decimal b) lightOrange = (0.993m, 0.964m, 0.929m);

                var filledColors = filledRects
                    .OrderBy(x => x.GetBoundingRectangle().Value.Left)
                    .ThenByDescending(x => x.GetBoundingRectangle().Value.Top)
                    .Select(x => x.FillColor).ToArray();

                for (int r = 0; r < filledColors.Length; r++)
                {
                    var color = filledColors[r];
                    Assert.Equal(PdfPig.Graphics.Colors.ColorSpace.DeviceRGB, color.ColorSpace);

                    if (r % 2 == 0)
                    {
                        if (r == 2)
                        {
                            Assert.Equal(lightRed2, color.ToRGBValues());
                        }
                        else
                        {
                            Assert.Equal(lightRed, color.ToRGBValues());
                        }
                    }
                    else
                    {
                        Assert.Equal(lightOrange, color.ToRGBValues());
                    }
                }
            }
        }
    }
}
