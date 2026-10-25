namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using Outline.Destinations;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tests.Tokens;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    /// <summary>
    /// PDF 32000-1 §7.8.3: resource names are local to the content stream whose resource dictionary
    /// declares them. A form XObject that reuses a name already defined by the page must shadow the
    /// page's entry only while that form is being processed.
    /// </summary>
    public class ResourceScopingTests
    {
        private const string AnyDocument = "Single Page Simple - from open office.pdf";

        private static readonly NameToken Sh0 = NameToken.Create("Sh0");
        private static readonly NameToken P0 = NameToken.Create("P0");
        private static readonly NameToken Mc0 = NameToken.Create("MC0");
        private static readonly NameToken Level = NameToken.Create("Level");

        private static readonly NameToken F1 = NameToken.Create("F1");

        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        /// <summary>
        /// Returns a new font named after the dictionary's /BaseFont, so tests can tell inline fonts apart.
        /// </summary>
        private sealed class InlineFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) =>
                new NamedFont(dictionary.TryGet(NameToken.BaseFont, out NameToken? baseFont) ? baseFont : null);
        }

        private sealed class NamedFont(NameToken? name) : IFont
        {
            public NameToken? Name { get; } = name;

            public bool IsVertical => false;

            // Null rather than throwing: loading an indirect font stamps its reference through Details?.
            public FontDetails Details => null!;

            public int ReadCharacterCode(IInputBytes bytes, out int codeLength) => throw new System.NotSupportedException();

            public bool TryGetUnicode(int characterCode, out string? value) => throw new System.NotSupportedException();

            public CharacterBoundingBox GetBoundingBox(int characterCode) => throw new System.NotSupportedException();

            public TransformationMatrix GetFontMatrix() => throw new System.NotSupportedException();

            public double GetDescent() => throw new System.NotSupportedException();

            public double GetAscent() => throw new System.NotSupportedException();

            public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath>? path) => throw new System.NotSupportedException();

            public bool TryGetNormalisedPath(int characterCode, out IReadOnlyList<PdfSubpath>? path) => throw new System.NotSupportedException();

            public bool TryGetGlyphIndex(int characterCode, out int glyphIndex) => throw new System.NotSupportedException();
        }

        private static DictionaryToken InlineFont(string baseFont) => Dict(
            (NameToken.Type, NameToken.Font),
            (NameToken.BaseFont, NameToken.Create(baseFont)));

        private static ResourceStore CreateStore(IPdfTokenScanner scanner, IFontFactory? fontFactory = null) => new ResourceStore(
            scanner,
            fontFactory ?? new NoOpFontFactory(),
            new FilterProviderWithLookup(DefaultFilterProvider.Instance),
            null,
            new ParsingOptions
            {
                UseLenientParsing = true,
                SkipMissingFonts = true,
            });

        private static DictionaryToken Dict(params (NameToken Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var entry in entries)
            {
                data[entry.Key] = entry.Value;
            }

            return new DictionaryToken(data);
        }

        private static ArrayToken Numbers(params double[] values)
        {
            var tokens = new List<IToken>(values.Length);
            foreach (var value in values)
            {
                tokens.Add(new NumericToken(value));
            }

            return new ArrayToken(tokens);
        }

        private static DictionaryToken ExponentialFunction() => Dict(
            (NameToken.FunctionType, new NumericToken(2)),
            (NameToken.Domain, Numbers(0, 1)),
            (NameToken.C0, Numbers(0, 0, 0)),
            (NameToken.C1, Numbers(1, 1, 1)),
            (NameToken.N, new NumericToken(1)));

        /// <summary>
        /// A minimal axial (type 2) or radial (type 3) shading, whose runtime type is what the tests
        /// assert on to tell "the page's /Sh0" apart from "the form's /Sh0".
        /// </summary>
        private static DictionaryToken Shading(int shadingType) => Dict(
            (NameToken.ShadingType, new NumericToken(shadingType)),
            (NameToken.ColorSpace, NameToken.Devicergb),
            (NameToken.Coords, shadingType == 2 ? Numbers(0, 0, 1, 0) : Numbers(0, 0, 0, 0, 0, 1)),
            (NameToken.Function, ExponentialFunction()));

        private static DictionaryToken ShadingPattern(int shadingType) => Dict(
            (NameToken.PatternType, new NumericToken(2)),
            (NameToken.Shading, Shading(shadingType)));

        /// <summary>
        /// A resource dictionary defining /Sh0, /P0 and /MC0. Both the outer ("page") and inner
        /// ("form") dictionaries use the same names, differing only in <paramref name="shadingType"/>.
        /// </summary>
        private static DictionaryToken Resources(int shadingType) => Dict(
            (NameToken.Shading, Dict((Sh0, Shading(shadingType)))),
            (NameToken.Pattern, Dict((P0, ShadingPattern(shadingType)))),
            (NameToken.Properties, Dict((Mc0, Dict((Level, new NumericToken(shadingType)))))));

        [Fact]
        public void FormShadingDoesNotOutliveItsResourceDictionary()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(AnyDocument));

            var store = CreateStore(document.Structure.TokenScanner);

            store.LoadResourceDictionary(Resources(2));
            var pageShading = store.GetShading(Sh0);
            Assert.IsType<AxialShading>(pageShading);

            store.LoadResourceDictionary(Resources(3));
            Assert.IsType<RadialShading>(store.GetShading(Sh0));

            store.UnloadResourceDictionary();
            Assert.Same(pageShading, store.GetShading(Sh0));

            store.UnloadResourceDictionary();
        }

        [Fact]
        public void FormPatternDoesNotOutliveItsResourceDictionary()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(AnyDocument));

            var store = CreateStore(document.Structure.TokenScanner);

            store.LoadResourceDictionary(Resources(2));
            var pagePattern = Assert.IsType<ShadingPatternColor>(store.GetPatterns()[P0]);
            Assert.IsType<AxialShading>(pagePattern.Shading);

            store.LoadResourceDictionary(Resources(3));
            Assert.IsType<RadialShading>(Assert.IsType<ShadingPatternColor>(store.GetPatterns()[P0]).Shading);

            store.UnloadResourceDictionary();
            Assert.Same(pagePattern, store.GetPatterns()[P0]);

            store.UnloadResourceDictionary();
        }

        [Fact]
        public void FormMarkedContentPropertiesDoNotOutliveTheirResourceDictionary()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(AnyDocument));

            var store = CreateStore(document.Structure.TokenScanner);

            store.LoadResourceDictionary(Resources(2));
            Assert.Equal(2, GetLevel(store));

            store.LoadResourceDictionary(Resources(3));
            Assert.Equal(3, GetLevel(store));

            store.UnloadResourceDictionary();
            Assert.Equal(2, GetLevel(store));

            store.UnloadResourceDictionary();

            static int GetLevel(ResourceStore store) =>
                ((NumericToken)store.GetMarkedContentPropertiesDictionary(Mc0)!.Data[Level.Data]).Int;
        }

        /// <summary>
        /// Callers unload only the dictionaries that loaded, so one that fails part way through loading
        /// must not leave its level on the stack above the enclosing dictionary.
        /// </summary>
        [Fact]
        public void FailedLoadDoesNotLeaveItsLevelBehind()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(AnyDocument));

            var store = CreateStore(document.Structure.TokenScanner);

            store.LoadResourceDictionary(Resources(2));

            // Shadings are loaded after the other categories are pushed, and a shading that is
            // neither a dictionary nor a stream is rejected.
            var invalid = Dict(
                (NameToken.Properties, Dict((Mc0, Dict((Level, new NumericToken(3)))))),
                (NameToken.Shading, Dict((Sh0, new NumericToken(1)))));

            Assert.ThrowsAny<System.Exception>(() => store.LoadResourceDictionary(invalid));

            Assert.Equal(2, ((NumericToken)store.GetMarkedContentPropertiesDictionary(Mc0)!.Data[Level.Data]).Int);

            store.UnloadResourceDictionary();
        }

        /// <summary>
        /// Fonts written inline in a resource dictionary are scoped like every other resource: a form
        /// that reuses the page's inline font name shadows it only until the form's dictionary unloads.
        /// </summary>
        [Fact]
        public void InlineFontIsRestoredAfterNestedDictionaryUnloads()
        {
            var store = CreateStore(new TestPdfTokenScanner(), new InlineFontFactory());

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, InlineFont("PageFont"))))));
            var pageFont = store.GetFont(F1);
            Assert.Equal("PageFont", pageFont?.Name?.Data);

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, InlineFont("FormFont"))))));
            Assert.Equal("FormFont", store.GetFont(F1)?.Name?.Data);

            store.UnloadResourceDictionary();
            Assert.Same(pageFont, store.GetFont(F1));

            store.UnloadResourceDictionary();
            Assert.Null(store.GetFont(F1));
        }

        /// <summary>
        /// The innermost dictionary that defines a font name wins, whether the font is written inline or
        /// referenced indirectly at either level.
        /// </summary>
        [Fact]
        public void NestedInlineFontShadowsOuterIndirectFont()
        {
            var scanner = new TestPdfTokenScanner();
            var pageFontReference = new IndirectReference(20, 0);
            scanner.Objects[pageFontReference] = new ObjectToken(XrefLocation.File(0), pageFontReference, InlineFont("PageFont"));

            var store = CreateStore(scanner, new InlineFontFactory());

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, new IndirectReferenceToken(pageFontReference))))));
            var pageFont = store.GetFont(F1);
            Assert.Equal("PageFont", pageFont?.Name?.Data);

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, InlineFont("FormFont"))))));
            Assert.Equal("FormFont", store.GetFont(F1)?.Name?.Data);

            store.UnloadResourceDictionary();
            Assert.Same(pageFont, store.GetFont(F1));

            store.UnloadResourceDictionary();
        }

        [Fact]
        public void NestedIndirectFontShadowsOuterInlineFont()
        {
            var scanner = new TestPdfTokenScanner();
            var formFontReference = new IndirectReference(21, 0);
            scanner.Objects[formFontReference] = new ObjectToken(XrefLocation.File(0), formFontReference, InlineFont("FormFont"));

            var store = CreateStore(scanner, new InlineFontFactory());

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, InlineFont("PageFont"))))));
            var pageFont = store.GetFont(F1);

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, new IndirectReferenceToken(formFontReference))))));
            Assert.Equal("FormFont", store.GetFont(F1)?.Name?.Data);

            store.UnloadResourceDictionary();
            Assert.Same(pageFont, store.GetFont(F1));

            store.UnloadResourceDictionary();
        }

        /// <summary>
        /// A dictionary that fails part way through loading must not leave its inline fonts bound either.
        /// </summary>
        [Fact]
        public void FailedLoadDoesNotLeaveItsInlineFontsBehind()
        {
            var store = CreateStore(new TestPdfTokenScanner(), new InlineFontFactory());

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict((F1, InlineFont("PageFont"))))));
            var pageFont = store.GetFont(F1);

            var invalid = Dict(
                (NameToken.Font, Dict((F1, InlineFont("FormFont")))),
                (NameToken.Shading, Dict((Sh0, new NumericToken(1)))));

            Assert.ThrowsAny<System.Exception>(() => store.LoadResourceDictionary(invalid));

            Assert.Same(pageFont, store.GetFont(F1));

            store.UnloadResourceDictionary();
        }

        /// <summary>
        /// A form that does not redefine a name still resolves it against the enclosing dictionary,
        /// matching how fonts / XObjects / ExtGStates / colour spaces already behave.
        /// </summary>
        [Fact]
        public void NestedResourceDictionaryFallsBackToOuterEntry()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(AnyDocument));

            var store = CreateStore(document.Structure.TokenScanner);

            store.LoadResourceDictionary(Resources(2));
            var pageShading = store.GetShading(Sh0);
            var pagePattern = store.GetPatterns()[P0];

            store.LoadResourceDictionary(Dict((NameToken.Font, Dict())));
            Assert.Same(pageShading, store.GetShading(Sh0));
            Assert.Same(pagePattern, store.GetPatterns()[P0]);

            store.UnloadResourceDictionary();
            store.UnloadResourceDictionary();
        }

        /// <summary>
        /// End-to-end version of <see cref="FormShadingDoesNotOutliveItsResourceDictionary"/>, driving
        /// the real content stream of the document the defect was found on.
        /// <para>
        /// Page 1 of 0000851.pdf declares /Sh0 as an axial gradient and paints it with ~119 `sh`
        /// operators (the cloud highlights in the banner). The very first thing the page draws is a
        /// form XObject that declares its <i>own</i> /Sh0 - a type 7 tensor-product mesh - so every
        /// `sh` after that form returned used to resolve to the mesh, painting the whole sky artwork
        /// clipped to each cloud outline.
        /// </para>
        /// </summary>
        [Fact]
        public void ShadingNameIsRestoredAfterFormXObjectInRealDocument()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("0000851.pdf"));
            document.AddPageFactory<ShadingUsagePage, ShadingUsagePageFactory>();

            var usages = document.GetPage<ShadingUsagePage>(1).Usages;

            var pageLevel = usages.Where(u => u.Name.Equals(Sh0) && u.FormDepth == 0).ToList();
            var insideForms = usages.Where(u => u.Name.Equals(Sh0) && u.FormDepth > 0).ToList();

            // Sanity: this page really does paint /Sh0 many times at page level, and really does
            // redefine the name inside a form - otherwise the assertions below prove nothing.
            Assert.True(pageLevel.Count > 100, $"Expected many page-level /Sh0 paints, got {pageLevel.Count}.");
            Assert.Contains(insideForms, u => u.Shading is TensorProductPatchMeshesShading);

            // The bug: after the first form XObject returned, page-level /Sh0 resolved to that form's
            // mesh instead of the page's axial shading.
            Assert.All(pageLevel, u => Assert.IsType<AxialShading>(u.Shading));
            Assert.All(pageLevel, u => Assert.Same(pageLevel[0].Shading, u.Shading));
        }

        #region Shading-usage page factory

        public readonly struct ShadingUsage(NameToken name, Shading shading, int formDepth)
        {
            /// <summary>The name the `sh` operator asked for.</summary>
            public NameToken Name { get; } = name;

            /// <summary>What the resource store resolved it to.</summary>
            public Shading Shading { get; } = shading;

            /// <summary>0 when painted by the page's own content stream, &gt; 0 inside a form XObject.</summary>
            public int FormDepth { get; } = formDepth;
        }

        public readonly struct ShadingUsagePage(int number, IReadOnlyList<ShadingUsage> usages)
        {
            public int Number { get; } = number;

            public IReadOnlyList<ShadingUsage> Usages { get; } = usages;
        }

        public class ShadingUsagePageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ParsingOptions parsingOptions)
            : BasePageFactory<ShadingUsagePage>(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
        {
            protected override ShadingUsagePage ProcessPage(int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations,
                CancellationToken token)
            {
                if (operations.Count == 0)
                {
                    return new ShadingUsagePage(pageNumber, []);
                }

                var context = new ShadingUsageStreamProcessor(pageNumber, ResourceStore, PdfScanner,
                    PageContentParser, FilterProvider, cropBox, userSpaceUnit, rotation, initialMatrix,
                    ParsingOptions, token);

                return new ShadingUsagePage(pageNumber, context.Process(pageNumber, operations));
            }
        }

        /// <summary>
        /// Does nothing but record, for every `sh` operator, which <see cref="Shading"/> the resource
        /// store resolves the name to and how deep inside form XObjects the operator sits.
        /// </summary>
        public sealed class ShadingUsageStreamProcessor(int pageNumber,
            IResourceStore resourceStore,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            ParsingOptions parsingOptions,
            CancellationToken token)
            : BaseStreamProcessor<IReadOnlyList<ShadingUsage>>(pageNumber, resourceStore, pdfScanner,
                pageContentParser, filterProvider, cropBox, userSpaceUnit, rotation, initialMatrix, null, parsingOptions, token)
        {
            private readonly List<ShadingUsage> _usages = [];
            private int _formDepth;

            public override IReadOnlyList<ShadingUsage> Process(int pageNumberCurrent,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                CloneAllStates();
                ProcessOperations(operations);
                return _usages;
            }

            public override void PaintShading(NameToken shadingName)
            {
                _usages.Add(new ShadingUsage(shadingName, ResourceStore.GetShading(shadingName), _formDepth));
            }

            protected override void ProcessFormXObject(StreamToken formStream, NameToken xObjectName)
            {
                _formDepth++;
                try
                {
                    base.ProcessFormXObject(formStream, xObjectName);
                }
                finally
                {
                    _formDepth--;
                }
            }

            public override void RenderGlyph(IFont font, CurrentGraphicsState currentState, double fontSize,
                double pointSize, int code, string unicode, long currentOffset,
                in TransformationMatrix renderingMatrix, in TransformationMatrix textMatrix,
                in TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox)
            {
            }

            protected override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
            {
            }

            protected override void RenderInlineImage(InlineImage inlineImage)
            {
            }

            public override void BeginSubpath()
            {
            }

            public override PdfPoint? CloseSubpath() => new PdfPoint();

            public override void StrokePath(bool close)
            {
            }

            public override void FillPath(FillingRule fillingRule, bool close)
            {
            }

            public override void FillStrokePath(FillingRule fillingRule, bool close)
            {
            }

            public override void MoveTo(double x, double y)
            {
            }

            public override void BezierCurveTo(double x2, double y2, double x3, double y3)
            {
            }

            public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
            {
            }

            public override void LineTo(double x, double y)
            {
            }

            public override void Rectangle(double x, double y, double width, double height)
            {
            }

            public override void EndPath()
            {
            }

            public override void ClosePath()
            {
            }

            public override void ModifyClippingIntersect(FillingRule clippingRule)
            {
            }

            protected override void ClipToRectangle(PdfRectangle rectangle, FillingRule clippingRule)
            {
            }

            public override void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName,
                DictionaryToken properties)
            {
            }

            public override void EndMarkedContent()
            {
            }
        }

        #endregion
    }
}
