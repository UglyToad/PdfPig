namespace UglyToad.PdfPig.Tests.Artwork
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Artwork;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;

    public class Artwork3DViewTests
    {
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();

        internal static DictionaryToken Dict(params (string Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var (key, value) in entries)
            {
                data[NameToken.Create(key)] = value;
            }

            return new DictionaryToken(data);
        }

        internal static ArrayToken Num(params double[] values)
        {
            var tokens = new IToken[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                tokens[i] = new NumericToken(values[i]);
            }

            return new ArrayToken(tokens);
        }

        internal static ArrayToken Arr(params IToken[] tokens) => new ArrayToken(tokens);

        internal static NameToken Name(string name) => NameToken.Create(name);

        internal static StringToken Text(string text) => new StringToken(text);

        private static Artwork3DView Parse(DictionaryToken dictionary)
        {
            Assert.True(Artwork3DParser.TryParseView(dictionary, Scanner, out var view));
            return view;
        }

        [Fact]
        public void ParsesMatrixView()
        {
            var view = Parse(Dict(("XN", Text("Iso")), ("MS", Name("M")),
                ("C2W", Num(1, 0, 0, 0, 1, 0, 0, 0, 1, 10, 20, 30)), ("CO", new NumericToken(42.5))));

            Assert.Equal("Iso", view.ExternalName);
            Assert.Equal("Iso", view.InternalName);
            Assert.Equal(Artwork3DMatrixSource.Matrix, view.MatrixSource);
            Assert.Equal(30, view.CameraToWorld!.Value.Tz);
            Assert.Equal(42.5, view.CenterOfOrbit);
            Assert.Empty(view.Nodes);
            Assert.Empty(view.CrossSections);
            Assert.Empty(view.U3DPath);
            Assert.Empty(view.Measurements);
            Assert.False(view.ResetNodes);
            Assert.Null(view.Projection);
            Assert.Null(view.Background);
            Assert.Null(view.RenderMode);
            Assert.Null(view.LightingScheme);
            Assert.Null(view.Overlay);
        }

        [Fact]
        public void InternalNameIsKeptWhenPresent()
        {
            var view = Parse(Dict(("XN", Text("Front")), ("IN", Text("id-front"))));

            Assert.Equal("Front", view.ExternalName);
            Assert.Equal("id-front", view.InternalName);
            Assert.Equal(Artwork3DMatrixSource.None, view.MatrixSource);
        }

        [Fact]
        public void ShortMatrixIsAbsent()
        {
            var view = Parse(Dict(("XN", Text("v")), ("MS", Name("M")), ("C2W", Num(1, 0, 0, 0, 1, 0, 0, 0, 1, 10, 20))));

            Assert.Null(view.CameraToWorld);
        }

        [Fact]
        public void ParsesU3DPathAsStringOrArray()
        {
            var single = Parse(Dict(("XN", Text("v")), ("MS", Name("U3D")), ("U3DPath", Text("DefaultView"))));
            var several = Parse(Dict(("XN", Text("v")), ("MS", Name("U3D")), ("U3DPath", Arr(Text("a"), Text("b")))));

            Assert.Equal(Artwork3DMatrixSource.U3DPath, single.MatrixSource);
            Assert.Equal(new[] { "DefaultView" }, single.U3DPath);
            Assert.Equal(new[] { "a", "b" }, several.U3DPath);
        }

        [Fact]
        public void ProjectionDefaultsAndVariants()
        {
            var p = Parse(Dict(("XN", Text("v")), ("P", Dict(("Subtype", Name("P")), ("FOV", new NumericToken(30)))))).Projection!;
            Assert.Equal(Artwork3DProjectionType.Perspective, p.Type);
            Assert.Equal(30, p.FieldOfView);
            Assert.Equal(Artwork3DScaling.Width, p.PerspectiveScaling);
            Assert.Null(p.PerspectiveScalingValue);
            Assert.Equal(Artwork3DClippingStyle.AutomaticNearFar, p.ClippingStyle);
            Assert.Equal(1, p.OrthographicScale);
            Assert.Equal(Artwork3DScaling.Absolute, p.OrthographicBinding);

            var o = Parse(Dict(("XN", Text("v")), ("P", Dict(("Subtype", Name("O")),
                ("OS", new NumericToken(0.0028)), ("OB", Name("Min")))))).Projection!;
            Assert.Equal(Artwork3DProjectionType.Orthographic, o.Type);
            Assert.Equal(0.0028, o.OrthographicScale);
            Assert.Equal(Artwork3DScaling.Min, o.OrthographicBinding);

            var n = Parse(Dict(("XN", Text("v")), ("P", Dict(("Subtype", Name("P")), ("PS", new NumericToken(1)),
                ("CS", Name("XNF")), ("N", new NumericToken(0.5)), ("F", new NumericToken(900)))))).Projection!;
            Assert.Equal(Artwork3DScaling.Value, n.PerspectiveScaling);
            Assert.Equal(1, n.PerspectiveScalingValue);
            Assert.Equal(Artwork3DClippingStyle.ExplicitNearFar, n.ClippingStyle);
            Assert.Equal(0.5, n.Near);
            Assert.Equal(900, n.Far);

            var h = Parse(Dict(("XN", Text("v")), ("P", Dict(("Subtype", Name("P")), ("PS", Name("H")))))).Projection!;
            Assert.Equal(Artwork3DScaling.Height, h.PerspectiveScaling);
        }

        [Fact]
        public void BackgroundRenderModeLighting()
        {
            var v = Parse(Dict(("XN", Text("v")),
                ("BG", Dict(("Subtype", Name("SC")), ("C", Num(0.25, 0.5, 0.75)), ("EA", BooleanToken.True))),
                ("RM", Dict(("Subtype", Name("SolidOutline")), ("CV", new NumericToken(10)),
                    ("AC", Arr(Name("DeviceRGB"), new NumericToken(1), new NumericToken(0), new NumericToken(0))),
                    ("FC", Name("BG")), ("O", new NumericToken(0.3)))),
                ("LS", Dict(("Subtype", Name("CAD"))))));

            Assert.Equal(0.25, v.Background!.Color.R);
            Assert.Equal(0.5, v.Background.Color.G);
            Assert.Equal(0.75, v.Background.Color.B);
            Assert.True(v.Background.EntireAnnotation);
            Assert.Equal(Artwork3DRenderModeType.SolidOutline, v.RenderMode!.Type);
            Assert.Equal("SolidOutline", v.RenderMode.Name);
            Assert.Equal(10, v.RenderMode.CreaseValue);
            Assert.Equal(new Artwork3DColor(1, 0, 0), v.RenderMode.AuxiliaryColor);
            Assert.True(v.RenderMode.FaceColorIsBackground);
            Assert.Null(v.RenderMode.FaceColor);
            Assert.Equal(0.3, v.RenderMode.Opacity);
            Assert.Equal(Artwork3DLightingStyle.CAD, v.LightingScheme!.Style);
        }

        [Fact]
        public void BackgroundDefaults()
        {
            var v = Parse(Dict(("XN", Text("v")), ("BG", Dict(("Subtype", Name("SC"))))));

            Assert.Equal(Artwork3DColor.White, v.Background!.Color);
            Assert.False(v.Background.EntireAnnotation);
        }

        [Fact]
        public void UnknownRenderModeAndLightingKeepTheirNames()
        {
            var v = Parse(Dict(("XN", Text("v")), ("RM", Dict(("Subtype", Name("Sparkly")))), ("LS", Dict(("Subtype", Name("Disco"))))));

            Assert.Equal(Artwork3DRenderModeType.Unknown, v.RenderMode!.Type);
            Assert.Equal("Sparkly", v.RenderMode.Name);
            Assert.Equal(Artwork3DLightingStyle.Unknown, v.LightingScheme!.Style);
            Assert.Equal("Disco", v.LightingScheme.Name);
        }

        [Fact]
        public void NodesKeepAbsentAsNull()
        {
            var v = Parse(Dict(("XN", Text("v")), ("NR", BooleanToken.True), ("NA", Arr(
                Dict(("N", Text("part-1")), ("V", BooleanToken.False)),
                Dict(("N", Text("part-2")), ("O", new NumericToken(0.5)), ("M", Num(1, 0, 0, 0, 1, 0, 0, 0, 1, 1, 2, 3)),
                    ("RM", Dict(("Subtype", Name("Transparent"))))),
                Dict(("V", BooleanToken.True)),
                new NumericToken(7)))));

            Assert.True(v.ResetNodes);
            Assert.Equal(2, v.Nodes.Count);
            Assert.Equal("part-1", v.Nodes[0].Name);
            Assert.False(v.Nodes[0].Visible);
            Assert.Null(v.Nodes[0].Opacity);
            Assert.Null(v.Nodes[0].Matrix);
            Assert.Null(v.Nodes[0].RenderMode);
            Assert.Null(v.Nodes[1].Visible);
            Assert.Equal(0.5, v.Nodes[1].Opacity);
            Assert.Equal(3, v.Nodes[1].Matrix!.Value.Tz);
            Assert.Equal(Artwork3DRenderModeType.Transparent, v.Nodes[1].RenderMode!.Type);
        }

        [Fact]
        public void CrossSectionOrientationKeepsNulls()
        {
            var v = Parse(Dict(("XN", Text("v")), ("SA", Arr(Dict(
                ("C", Num(1, 2, 3)),
                ("O", Arr(NullToken.Instance, new NumericToken(0), new NumericToken(90))),
                ("PO", new NumericToken(0.5)),
                ("PC", Arr(Name("DeviceRGB"), new NumericToken(0), new NumericToken(1), new NumericToken(0))),
                ("IV", BooleanToken.True))))));

            var s = Assert.Single(v.CrossSections);
            Assert.Equal(new double[] { 1, 2, 3 }, s.Center);
            Assert.Equal(new double?[] { null, 0, 90 }, s.Orientation);
            Assert.Equal(0.5, s.PlaneOpacity);
            Assert.Equal(new Artwork3DColor(0, 1, 0), s.PlaneColor);
            Assert.True(s.IntersectionVisible);
            Assert.Null(s.IntersectionColor);
            Assert.Null(s.ShowTransparent);
            Assert.Null(s.SectionCapping);
        }

        [Fact]
        public void WrongTypesBecomeAbsent()
        {
            var v = Parse(Dict(("XN", new NumericToken(3)), ("MS", Text("M")), ("C2W", Text("nope")), ("CO", Name("far")),
                ("P", new NumericToken(1)), ("BG", Arr()), ("RM", Text("Solid")), ("LS", BooleanToken.True),
                ("SA", Dict()), ("NA", Text("x")), ("NR", new NumericToken(1))));

            Assert.Null(v.ExternalName);
            Assert.Null(v.InternalName);
            Assert.Equal(Artwork3DMatrixSource.None, v.MatrixSource);
            Assert.Null(v.CameraToWorld);
            Assert.Null(v.CenterOfOrbit);
            Assert.Null(v.Projection);
            Assert.Null(v.Background);
            Assert.Null(v.RenderMode);
            Assert.Null(v.LightingScheme);
            Assert.Empty(v.CrossSections);
            Assert.Empty(v.Nodes);
            Assert.False(v.ResetNodes);
        }

        [Fact]
        public void ViewArraySkipsBadEntriesAndResolvesReferences()
        {
            var scanner = new TestPdfTokenScanner();
            var reference = new IndirectReference(9, 0);
            scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference, Dict(("XN", Text("indirect"))));

            var views = Artwork3DParser.ParseViews(Arr(
                Dict(("XN", Text("first"))),
                new NumericToken(3),
                new IndirectReferenceToken(reference),
                Dict(("MS", Name("M")))), scanner);

            Assert.Equal(new[] { "first", "indirect", null }, views.Select(x => x.ExternalName));
        }

        [Fact]
        public void MissingViewArrayIsEmpty()
        {
            Assert.Empty(Artwork3DParser.ParseViews(null, Scanner));
        }

        [Fact]
        public void DanglingReferenceIsNotAView()
        {
            // The test scanner throws for an unknown object; a real scanner returns null. Neither may escape.
            Assert.False(Artwork3DParser.TryParseView(new IndirectReferenceToken(new IndirectReference(404, 0)), Scanner, out _));
        }

        [Fact]
        public void NodeArrayIsCapped()
        {
            var nodes = new IToken[Artwork3DParser.MaxNodes + 5];
            var node = Dict(("N", Text("n")));
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = node;
            }

            var view = Parse(Dict(("XN", Text("v")), ("NA", new ArrayToken(nodes))));

            Assert.Equal(Artwork3DParser.MaxNodes, view.Nodes.Count);
        }
    }
}
