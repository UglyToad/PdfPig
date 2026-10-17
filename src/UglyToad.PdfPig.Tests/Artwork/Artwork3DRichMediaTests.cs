namespace UglyToad.PdfPig.Tests.Artwork
{
    using System.Linq;
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Artwork;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using static Artwork3DTests;
    using static Artwork3DViewTests;

    public class Artwork3DRichMediaTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();
        private static readonly CountingFilterProvider Filters = new CountingFilterProvider();
        private static readonly byte[] SwfBytes = { (byte)'C', (byte)'W', (byte)'S', 0x0B };

        private static Annotation RichMedia(DictionaryToken dictionary) => AnnotationOfType(AnnotationType.RichMedia, dictionary);

        private static DictionaryToken Asset(string fileName, byte[] data, string streamKey = "F")
        {
            return Dict(("F", Text(fileName)), ("EF", Dict((streamKey, Stream(data)))));
        }

        private static DictionaryToken Configuration(string subtype, params DictionaryToken[] assets)
        {
            var instances = Arr(assets.Select(a => (IToken)Dict(("Asset", a))).ToArray());
            return subtype is null
                ? Dict(("Instances", instances))
                : Dict(("Subtype", Name(subtype)), ("Instances", instances));
        }

        private static Annotation WithConfigurations(params DictionaryToken[] configurations)
        {
            return RichMedia(Dict(("RichMediaContent", Dict(("Configurations", Arr(configurations))))));
        }

        [Fact]
        public void FindsThe3DAssetAndItsViews()
        {
            var annotation = RichMedia(Dict(
                ("RichMediaContent", Dict(
                    ("Configurations", Arr(Configuration("3D", Asset("Grinder.prc", PrcBytes)))),
                    ("Views", Arr(Dict(("XN", Text("Home"))), Dict(("XN", Text("Detail"))))))),
                ("RichMediaSettings", Dict(
                    ("Activation", Dict(("Condition", Name("PV")), ("Presentation", Dict(
                        ("Toolbar", BooleanToken.False), ("NavigationPane", BooleanToken.True),
                        ("Transparent", BooleanToken.True), ("Style", Name("Windowed")))))),
                    ("Deactivation", Dict(("Condition", Name("XD"))))))));
            var filters = new CountingFilterProvider();

            Assert.True(Artwork3D.TryCreate(annotation, Scanner, filters, out var a));

            Assert.Equal(Artwork3DSource.RichMedia, a.Source);
            Assert.Equal(Artwork3DFormat.Prc, a.Format);
            Assert.Equal("Grinder.prc", a.FormatName);
            Assert.Equal(0, filters.DecodeCount);
            Assert.Equal((byte)'P', a.GetData().Span[0]);
            Assert.Equal(new[] { "Home", "Detail" }, a.Views.Select(v => v.ExternalName));
            Assert.Equal("Home", a.DefaultView!.ExternalName);
            Assert.Equal(Artwork3DActivationMode.PageVisible, a.Activation.Mode);
            Assert.Equal(Artwork3DDeactivationMode.Explicit, a.Activation.DeactivationMode);
            Assert.False(a.Activation.ShowToolbar);
            Assert.True(a.Activation.ShowNavigationPanel);
            Assert.True(a.Activation.Transparent);
            Assert.Equal(Artwork3DPresentationStyle.Windowed, a.Activation.Style);
            Assert.Null(a.InstanceKey);
            Assert.True(a.IsInteractive);
            Assert.False(a.HasScript);
        }

        [Fact]
        public void ActivationViewIsTheDefaultView()
        {
            var annotation = RichMedia(Dict(
                ("RichMediaContent", Dict(
                    ("Configurations", Arr(Configuration("3D", Asset("a.u3d", U3dBytes)))),
                    ("Views", Arr(Dict(("XN", Text("Home"))))))),
                ("RichMediaSettings", Dict(("Activation", Dict(("View", Dict(("XN", Text("Start"))))))))));

            Assert.True(Artwork3D.TryCreate(annotation, Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.U3D, a.Format);
            Assert.Equal("Start", a.DefaultView!.ExternalName);
            Assert.Equal(Artwork3DActivationMode.Explicit, a.Activation.Mode);
        }

        [Fact]
        public void FlashOnlyIsNot3D()
        {
            Assert.False(Artwork3D.TryCreate(WithConfigurations(Configuration("Flash", Asset("demo.swf", SwfBytes))), Scanner, Filters, out _));
        }

        [Fact]
        public void The3DConfigurationWinsOverFlash()
        {
            Assert.True(Artwork3D.TryCreate(WithConfigurations(
                Configuration("Flash", Asset("demo.swf", SwfBytes)),
                Configuration("3D", Asset("part.PRC", PrcBytes))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.Prc, a.Format);
        }

        [Fact]
        public void ConfigurationWithoutSubtypeUsesTheAsset()
        {
            Assert.True(Artwork3D.TryCreate(WithConfigurations(Configuration(null, Asset("demo.swf", SwfBytes), Asset("x.prc", PrcBytes))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.Prc, a.Format);
            Assert.Equal("x.prc", a.FormatName);
        }

        [Fact]
        public void UnknownExtensionFallsBackToSignature()
        {
            Assert.True(Artwork3D.TryCreate(WithConfigurations(Configuration("3D", Asset("model.bin", U3dBytes))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.U3D, a.Format);
        }

        [Fact]
        public void UnknownExtensionAndSignatureIsNot3D()
        {
            Assert.False(Artwork3D.TryCreate(WithConfigurations(Configuration("3D", Asset("model.bin", SwfBytes))), Scanner, Filters, out _));
        }

        [Fact]
        public void EmbeddedFileUnderUnicodeKey()
        {
            Assert.True(Artwork3D.TryCreate(WithConfigurations(Configuration("3D", Asset("x.prc", PrcBytes, streamKey: "UF"))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.Prc, a.Format);
        }

        [Fact]
        public void MissingOrBrokenContentReturnsFalse()
        {
            Assert.False(Artwork3D.TryCreate(RichMedia(Dict()), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(RichMedia(Dict(("RichMediaContent", new NumericToken(1)))), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(WithConfigurations(), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(WithConfigurations(Dict(("Subtype", Name("3D")))), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(WithConfigurations(Configuration("3D", Dict(("F", Text("x.prc"))))), Scanner, Filters, out _));
        }
    }
}
