namespace UglyToad.PdfPig.Tests.Artwork
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Annotations;
    using UglyToad.PdfPig.Artwork;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using static Artwork3DViewTests;

    public class Artwork3DTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();
        private static readonly CountingFilterProvider Filters = new CountingFilterProvider();

        internal static readonly byte[] U3dBytes = { (byte)'U', (byte)'3', (byte)'D', 0, 0x18, 0, 0, 0 };
        internal static readonly byte[] PrcBytes = { (byte)'P', (byte)'R', (byte)'C', 0xB6, 0x1B, 0, 0 };

        internal static Annotation AnnotationOfType(AnnotationType type, DictionaryToken dictionary)
        {
            return new Annotation(dictionary, type, new PdfRectangle(0, 0, 100, 100), null, null, null, 0,
                AnnotationBorder.Default, new QuadPointsQuadrilateral[0], null, null, null, null, null, null);
        }

        private static Annotation Annotation3D(DictionaryToken dictionary) => AnnotationOfType(AnnotationType.Artwork3D, dictionary);

        internal static StreamToken Stream(byte[] data, params (string Key, IToken Value)[] entries)
        {
            return new StreamToken(Dict(entries), data);
        }

        private static StreamToken U3dStream(params (string Key, IToken Value)[] entries)
        {
            var all = new List<(string, IToken)> { ("Subtype", Name("U3D")) };
            all.AddRange(entries);
            return Stream(U3dBytes, all.ToArray());
        }

        private static ArrayToken ThreeViews()
        {
            return Arr(
                Dict(("XN", Text("Front")), ("IN", Text("id-front"))),
                Dict(("XN", Text("Back"))),
                Dict(("XN", Text("Top")), ("IN", Text("id-top"))));
        }

        private static Artwork3DView Default(IToken annotationView, IToken streamView)
        {
            var streamEntries = new List<(string, IToken)> { ("VA", ThreeViews()) };
            if (streamView != null)
            {
                streamEntries.Add(("DV", streamView));
            }

            var annotationEntries = new List<(string, IToken)> { ("3DD", U3dStream(streamEntries.ToArray())) };
            if (annotationView != null)
            {
                annotationEntries.Add(("3DV", annotationView));
            }

            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(annotationEntries.ToArray())), Scanner, Filters, out var artwork));
            return artwork.DefaultView;
        }

        [Fact]
        public void NotA3DAnnotationReturnsFalse()
        {
            Assert.False(Artwork3D.TryCreate(AnnotationOfType(AnnotationType.Link, Dict(("3DD", U3dStream()))), Scanner, Filters, out var artwork));
            Assert.Null(artwork);
        }

        [Fact]
        public void MissingOrWrong3DDReturnsFalse()
        {
            Assert.False(Artwork3D.TryCreate(Annotation3D(Dict()), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", new NumericToken(1)))), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", Dict(("Type", Name("3DRef")))))), Scanner, Filters, out _));
            Assert.False(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", new IndirectReferenceToken(new IndirectReference(404, 0))))), Scanner, Filters, out _));
        }

        [Fact]
        public void DirectStreamBasics()
        {
            var filters = new CountingFilterProvider();

            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", U3dStream()))), Scanner, filters, out var a));

            Assert.Equal(Artwork3DSource.Annotation3D, a.Source);
            Assert.Equal(Artwork3DFormat.U3D, a.Format);
            Assert.Equal("U3D", a.FormatName);
            Assert.Null(a.InstanceKey);
            Assert.Null(a.StreamReference);
            Assert.Equal(U3dBytes.Length, a.EncodedLength);
            Assert.Equal(0, filters.DecodeCount);

            var data = a.GetData();
            Assert.Equal(1, filters.DecodeCount);
            Assert.Equal((byte)'U', data.Span[0]);
            Assert.Equal(U3dBytes.Length, data.Length);

            Assert.Empty(a.Views);
            Assert.Null(a.DefaultView);
            Assert.True(a.IsInteractive);
            Assert.False(a.HasScript);
            Assert.True(a.GetScript().IsEmpty);
            Assert.Null(a.ViewBox);
            Assert.Null(a.Units);
            Assert.Null(a.ColorSpace);

            Assert.Equal(Artwork3DActivationMode.Explicit, a.Activation.Mode);
            Assert.Equal(Artwork3DInstanceState.Live, a.Activation.ActivationState);
            Assert.Equal(Artwork3DDeactivationMode.PageInvisible, a.Activation.DeactivationMode);
            Assert.Equal(Artwork3DInstanceState.Uninstantiated, a.Activation.DeactivationState);
            Assert.True(a.Activation.ShowToolbar);
            Assert.False(a.Activation.ShowNavigationPanel);
            Assert.Equal(Artwork3DPresentationStyle.Embedded, a.Activation.Style);
            Assert.False(a.Activation.Transparent);
            Assert.Null(a.Activation.Window);

            Assert.Equal(Artwork3DAnimationType.None, a.AnimationStyle.Type);
            Assert.Equal(0, a.AnimationStyle.PlayCount);
            Assert.Equal(1, a.AnimationStyle.TimeMultiplier);
        }

        [Theory]
        [InlineData("PRC", Artwork3DFormat.Prc)]
        [InlineData("U3D", Artwork3DFormat.U3D)]
        [InlineData("XYZ", Artwork3DFormat.Unknown)]
        public void FormatFromSubtype(string subtype, Artwork3DFormat expected)
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", Stream(PrcBytes, ("Subtype", Name(subtype)))))), Scanner, Filters, out var a));

            Assert.Equal(expected, a.Format);
            Assert.Equal(subtype, a.FormatName);
        }

        [Fact]
        public void MissingSubtypeIsUnknown()
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", Stream(PrcBytes)))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DFormat.Unknown, a.Format);
            Assert.Null(a.FormatName);
        }

        [Fact]
        public void SharedReferenceDictionary()
        {
            var scanner = new TestPdfTokenScanner();
            var streamReference = new IndirectReference(7, 0);
            var referenceDictionary = new IndirectReference(8, 0);
            scanner.Objects[streamReference] = new ObjectToken(XrefLocation.File(0), streamReference, Stream(PrcBytes, ("Subtype", Name("PRC"))));
            scanner.Objects[referenceDictionary] = new ObjectToken(XrefLocation.File(0), referenceDictionary,
                Dict(("Type", Name("3DRef")), ("3DD", new IndirectReferenceToken(streamReference))));

            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", new IndirectReferenceToken(referenceDictionary)))), scanner, Filters, out var a));
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", new IndirectReferenceToken(referenceDictionary)))), scanner, Filters, out var b));
            Assert.Equal(referenceDictionary, a.InstanceKey);
            Assert.Equal(a.InstanceKey, b.InstanceKey);
            Assert.Equal(streamReference, a.StreamReference);
            Assert.Equal(Artwork3DFormat.Prc, a.Format);

            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", new IndirectReferenceToken(streamReference)))), scanner, Filters, out var c));
            Assert.Null(c.InstanceKey);
            Assert.Equal(streamReference, c.StreamReference);
        }

        [Fact]
        public void DefaultViewPrecedence()
        {
            Assert.Equal("Front", Default(null, null).ExternalName);
            Assert.Equal("Back", Default(null, new NumericToken(1)).ExternalName);
            Assert.Equal("Top", Default(Text("id-top"), new NumericToken(1)).ExternalName);
            Assert.Equal("Back", Default(Text("Back"), null).ExternalName);
            Assert.Equal("Top", Default(Name("L"), null).ExternalName);
            Assert.Equal("Front", Default(Name("F"), new NumericToken(2)).ExternalName);
            Assert.Equal("Top", Default(Name("D"), new NumericToken(2)).ExternalName);
            Assert.Equal("Back", Default(Text("nope"), new NumericToken(1)).ExternalName);
            Assert.Equal("Front", Default(new NumericToken(99), new NumericToken(-1)).ExternalName);
            Assert.Equal("Front", Default(new NumericToken(1.5), null).ExternalName);
            Assert.Equal("Inline", Default(Dict(("XN", Text("Inline"))), null).ExternalName);
            Assert.Equal("StreamInline", Default(null, Dict(("XN", Text("StreamInline")))).ExternalName);
            Assert.Equal("Top", Default(null, Name("L")).ExternalName);
            Assert.Equal("Front", Default(null, Name("D")).ExternalName);
        }

        [Fact]
        public void NoViewsMeansNoDefaultView()
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", U3dStream(("DV", new NumericToken(0)))), ("3DV", Name("F")))), Scanner, Filters, out var a));

            Assert.Empty(a.Views);
            Assert.Null(a.DefaultView);
        }

        [Fact]
        public void ActivationAnimationAndFlags()
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(
                ("3DD", U3dStream(
                    ("AN", Dict(("Subtype", Name("Oscillating")), ("PC", new NumericToken(-1)), ("TM", new NumericToken(2)))),
                    ("OnInstantiate", Stream(new byte[] { 0x2F, 0x2F })))),
                ("3DA", Dict(("A", Name("PO")), ("AIS", Name("I")), ("D", Name("XD")), ("DIS", Name("I")),
                    ("TB", BooleanToken.False), ("NP", BooleanToken.True), ("Style", Name("Windowed")), ("Transparent", BooleanToken.True),
                    ("Window", Dict(("Width", Dict()))))),
                ("3DI", BooleanToken.False),
                ("3DU", Dict(("TSm", new NumericToken(1)))),
                ("3DB", Num(-10, -20, 10, 20)))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DActivationMode.PageOpen, a.Activation.Mode);
            Assert.Equal(Artwork3DInstanceState.Instantiated, a.Activation.ActivationState);
            Assert.Equal(Artwork3DDeactivationMode.Explicit, a.Activation.DeactivationMode);
            Assert.Equal(Artwork3DInstanceState.Instantiated, a.Activation.DeactivationState);
            Assert.False(a.Activation.ShowToolbar);
            Assert.True(a.Activation.ShowNavigationPanel);
            Assert.Equal(Artwork3DPresentationStyle.Windowed, a.Activation.Style);
            Assert.True(a.Activation.Transparent);
            Assert.NotNull(a.Activation.Window);

            Assert.Equal(Artwork3DAnimationType.Oscillating, a.AnimationStyle.Type);
            Assert.Equal(-1, a.AnimationStyle.PlayCount);
            Assert.Equal(2, a.AnimationStyle.TimeMultiplier);

            Assert.False(a.IsInteractive);
            Assert.True(a.HasScript);
            Assert.Equal(2, a.GetScript().Length);
            Assert.NotNull(a.Units);
            Assert.Equal(20, a.ViewBox!.Value.Width);
            Assert.Equal(40, a.ViewBox.Value.Height);
        }

        [Fact]
        public void PageVisibleActivation()
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", U3dStream()), ("3DA", Dict(("A", Name("PV")), ("D", Name("PC")), ("DIS", Name("L")))))), Scanner, Filters, out var a));

            Assert.Equal(Artwork3DActivationMode.PageVisible, a.Activation.Mode);
            Assert.Equal(Artwork3DDeactivationMode.PageClose, a.Activation.DeactivationMode);
            Assert.Equal(Artwork3DInstanceState.Live, a.Activation.DeactivationState);
        }

        [Fact]
        public void ViewsAreParsedOnce()
        {
            Assert.True(Artwork3D.TryCreate(Annotation3D(Dict(("3DD", U3dStream(("VA", ThreeViews()))))), Scanner, Filters, out var a));

            Assert.Equal(3, a.Views.Count);
            Assert.Same(a.Views, a.Views);
            Assert.Same(a.Views[0], a.DefaultView);
            Assert.Same(a.DefaultView, a.DefaultView);
        }
    }
}
