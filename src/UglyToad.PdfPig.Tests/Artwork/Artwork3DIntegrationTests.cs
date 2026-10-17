namespace UglyToad.PdfPig.Tests.Artwork
{
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Artwork;
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
            Assert.Equal(Artwork3DColor.White, view.Background!.Color);

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

        /// <summary>
        /// Local-only check against a folder of real 3D PDFs named by the CALY_3D_SAMPLES environment variable:
        /// every 3D or rich media annotation that yields artwork must hand out data starting with the signature
        /// of its format, and its views must enumerate without throwing. Skipped when the variable is not set.
        /// </summary>
        [SkippableFact]
        public void Corpus()
        {
            var root = Environment.GetEnvironmentVariable("CALY_3D_SAMPLES");
            Skip.If(string.IsNullOrEmpty(root) || !Directory.Exists(root), "CALY_3D_SAMPLES is not set to an existing folder.");

            int annotations = 0, artworks = 0, prc = 0, u3d = 0, views = 0, nodes = 0, richMedia = 0, posters = 0;
            foreach (var path in Directory.EnumerateFiles(root, "*.pdf", SearchOption.AllDirectories))
            {
                using var document = PdfDocument.Open(path);
                for (var p = 1; p <= document.NumberOfPages; p++)
                {
                    foreach (var annotation in document.GetPage(p).GetAnnotations())
                    {
                        if (annotation.Type != AnnotationType.Artwork3D && annotation.Type != AnnotationType.RichMedia)
                        {
                            continue;
                        }

                        annotations++;
                        Assert.True(Artwork3D.TryCreate(annotation, document.Structure.TokenScanner, document.Structure.FilterProvider, out var artwork), path);
                        artworks++;
                        if (artwork.Source == Artwork3DSource.RichMedia)
                        {
                            richMedia++;
                        }

                        if (annotation.HasNormalAppearance)
                        {
                            posters++;
                        }

                        var data = artwork.GetData().Span;
                        switch (artwork.Format)
                        {
                            case Artwork3DFormat.Prc:
                                prc++;
                                Assert.True(data[0] == 'P' && data[1] == 'R' && data[2] == 'C', path);
                                break;
                            case Artwork3DFormat.U3D:
                                u3d++;
                                Assert.True(data[0] == 'U' && data[1] == '3' && data[2] == 'D' && data[3] == 0, path);
                                break;
                            default:
                                Assert.Fail($"Unknown 3D format '{artwork.FormatName}' in {path}.");
                                break;
                        }

                        views += artwork.Views.Count;
                        foreach (var view in artwork.Views)
                        {
                            nodes += view.Nodes.Count;
                            _ = view.CrossSections.Count;
                        }

                        _ = artwork.DefaultView;
                        _ = artwork.GetScript().Length;
                    }
                }
            }

            output.WriteLine($"annotations={annotations} artworks={artworks} richMedia={richMedia} prc={prc} u3d={u3d} posters={posters} views={views} nodes={nodes}");
            Assert.True(artworks > 0, "No 3D artwork found under CALY_3D_SAMPLES.");
        }
    }
}
