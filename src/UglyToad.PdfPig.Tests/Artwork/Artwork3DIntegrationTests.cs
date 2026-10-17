namespace UglyToad.PdfPig.Tests.Artwork
{
    using System.Linq;
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Artwork;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Tests.Integration;
    using Xunit.Abstractions;

    public class Artwork3DIntegrationTests
    {
        private readonly ITestOutputHelper output;

        public Artwork3DIntegrationTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // box_base.pdf comes from the Universal 3D Sample Software (Samples/TestScenes), Apache License 2.0.
        [Fact]
        public void BoxBase()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("box_base.pdf"));

            var annotation = Assert.Single(document.GetPage(1).GetAnnotations().Where(x => x.Type == AnnotationType.Artwork3D));

            Assert.True(Artwork3D.TryCreate(annotation, document.Structure.TokenScanner, document.Structure.FilterProvider, out var artwork));

            Assert.Equal(Artwork3DSource.Annotation3D, artwork.Source);
            Assert.Equal(Artwork3DFormat.U3D, artwork.Format);
            Assert.Equal(1680, artwork.EncodedLength);
            Assert.NotNull(artwork.StreamReference);
            Assert.Null(artwork.InstanceKey);

            var data = artwork.GetData().Span;
            Assert.Equal(1680, data.Length);
            Assert.True(data[0] == 'U' && data[1] == '3' && data[2] == 'D' && data[3] == 0);

            var view = Assert.Single(artwork.Views);
            Assert.Equal("DefaultView", view.ExternalName);
            Assert.Equal("DefaultView", view.InternalName);
            Assert.Same(view, artwork.DefaultView);
            Assert.Equal(Artwork3DMatrixSource.U3DPath, view.MatrixSource);
            Assert.Equal(new[] { "DefaultView" }, view.U3DPath);
            Assert.Null(view.CameraToWorld);
            Assert.Equal(RGBColor.White, view.Background!.Color);

            Assert.Equal(Artwork3DActivationMode.PageVisible, artwork.Activation.Mode);
            Assert.True(artwork.Activation.ShowToolbar);
            Assert.True(artwork.Activation.ShowNavigationPanel);
            Assert.False(artwork.HasScript);

            // This file has an AP entry whose normal appearance is an empty dictionary: there is no poster.
            Assert.False(annotation.HasNormalAppearance);
        }

        [Fact]
        public void DocumentWithout3DHasNoArtwork()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Single Page Simple - from google drive.pdf"));

            foreach (var annotation in document.GetPage(1).GetAnnotations())
            {
                Assert.False(Artwork3D.TryCreate(annotation, document.Structure.TokenScanner, document.Structure.FilterProvider, out _));
            }
        }
    }
}
