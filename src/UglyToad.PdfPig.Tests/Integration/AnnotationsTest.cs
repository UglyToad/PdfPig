namespace UglyToad.PdfPig.Tests.Integration
{
    using Actions;
    using System.Linq;
    using Xunit;

    public class AnnotationsTest
    {
        [Fact]
        public void AnnotationsHaveActions()
        {
            var pdf = IntegrationHelpers.GetDocumentPath("toc");

            using (var doc = PdfDocument.Open(pdf))
            {
                var annots = doc.GetPage(1).ExperimentalAccess.GetAnnotations().ToArray();
                Assert.Equal(5, annots.Length);
                Assert.All(annots, a => Assert.NotNull(a.Action));
                Assert.All(annots, a => Assert.IsType<GoToAction>(a.Action));
                Assert.All(annots, a => Assert.True((a.Action as GoToAction).Destination.PageNumber > 0));
            }
        }

        [Fact]
        public void CheckAnnotationAppearanceStreams()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("appearances");
            using (var doc = PdfDocument.Open(pdf))
            {
                var annotations = doc.GetPage(1).ExperimentalAccess.GetAnnotations().ToArray();
                var annotation = Assert.Single(annotations);

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
