namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class AnnotationsTest
    {
        [Fact]
        public void AnnotationsHaveDestinations()
        {
            var pdf = IntegrationHelpers.GetDocumentPath("toc");

            using (var doc = PdfDocument.Open(pdf))
            {
                var annots = doc.GetPage(1).ExperimentalAccess.GetAnnotations().ToArray();
                Assert.Equal(5, annots.Length);
                Assert.All(annots, a => Assert.NotNull(a.Destination));
                Assert.All(annots, a => Assert.True(a.Destination.PageNumber > 0));
            }
        }

        [Fact]
        public void CheckAnnotationAppearanceStreams()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("appearances");
            using (var doc = PdfDocument.Open(pdf))
            {
                var annotations = doc.GetPage(1).ExperimentalAccess.GetAnnotations().ToArray();
                Assert.Equal(1, annotations.Length);
                var annotation = annotations[0];

                Assert.True(annotation.HasDownAppearance);
                Assert.True(annotation.HasNormalAppearance);
                Assert.False(annotation.HasRollOverAppearance);

                Assert.False(annotation.downAppearanceStream.IsStateless);
                Assert.False(annotation.normalAppearanceStream.IsStateless);

                Assert.Contains("Off", annotation.downAppearanceStream.GetStates);
                Assert.Contains("Yes", annotation.downAppearanceStream.GetStates);

                Assert.Contains("Off", annotation.normalAppearanceStream.GetStates);
                Assert.Contains("Yes", annotation.normalAppearanceStream.GetStates);
                
                Assert.Equal("Off", annotation.appearanceState);
            }
        }
    }
}
