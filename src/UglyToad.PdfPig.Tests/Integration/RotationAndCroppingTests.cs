namespace UglyToad.PdfPig.Tests.Integration
{
    public class RotationAndCroppingTests
    {
        [Fact]
        public void CroppedPageHasCorrectTextCoordinates()
        {
            var path = IntegrationHelpers.GetDocumentPath("SPARC - v9 Architecture Manual");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                Assert.Equal(612, page.Width);  // Due to cropping
                Assert.Equal(792, page.Height); // Due to cropping
                var minX = page.Letters.Select(l => l.BoundingBox.Left).Min();
                var maxX = page.Letters.Select(l => l.BoundingBox.Right).Max();
                Assert.Equal(74, minX, 0);  // If cropping is not applied correctly, these values will be off
                Assert.Equal(540, maxX, 0); // If cropping is not applied correctly, these values will be off
                // The page is cropped at 
                Assert.NotNull(page.Content);
            }
        }

        [Fact]
        public void WrongPathCount()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Publication_of_award_of_Bids_for_Transport_Sector__August_2016.pdf"),
                new ParsingOptions()
                {
                    ClipPaths = true
                }))
            {
                var page = document.GetPage(1);
                Assert.Equal(612, page.Height);
                Assert.Equal(224, page.Paths.Count);
            }
        }

        [Fact]
        public void Issue665()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("SmallCropbox.pdf")))
            {
                var page = document.GetPage(1);
                Assert.Equal(270, page.Rotation.Value); // Clockwise
                Assert.Equal(680, (int)page.Height);
                Assert.Equal(433, (int)page.Width);
                Assert.Equal(Content.PageSize.Custom, page.Size);
                Assert.Equal(2429, page.Letters.Count);

                var cropBox = page.CropBox.Bounds;
                Assert.Equal(0, cropBox.Rotation);
                Assert.Equal(680, (int)cropBox.Height);
                Assert.Equal(433, (int)cropBox.Width);
                Assert.Equal(0, (int)cropBox.Bottom);
                Assert.Equal(0, (int)cropBox.Left);
                Assert.Equal(433, (int)cropBox.Right);
                Assert.Equal(680, (int)cropBox.Top);

                var mediaBox = page.MediaBox.Bounds;
                Assert.Equal(0, mediaBox.Rotation);
                Assert.Equal(680, (int)mediaBox.Height);
                Assert.Equal(433, (int)mediaBox.Width);
                Assert.Equal(0, (int)mediaBox.Bottom);
                Assert.Equal(0, (int)mediaBox.Left);
                Assert.Equal(433, (int)mediaBox.Right);
                Assert.Equal(680, (int)mediaBox.Top);
            }
        }
    }
}
