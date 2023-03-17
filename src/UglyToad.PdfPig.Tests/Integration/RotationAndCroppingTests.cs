namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Xunit;

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
                var minX = page.Letters.Select(l => l.GlyphRectangle.Left).Min();
                var maxX = page.Letters.Select(l => l.GlyphRectangle.Right).Max();
                Assert.Equal(72, minX, 0);  // If cropping is not applied correctly, these values will be off
                Assert.Equal(540, maxX, 0); // If cropping is not applied correctly, these values will be off
                // The page is cropped at 
                Assert.NotNull(page.Content);
            }
        }
    }
}
