namespace UglyToad.PdfPig.Tests.Graphics
{
    using System.Collections.Generic;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors.Icc;
    using PdfPig.Graphics.Core;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.PdfFonts;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using Tokens;
    using Xunit;

    /// <summary>
    /// Which output intents a stream processor starts with (14.11.5). The processor is told; it never works
    /// them out. A page factory resolves them with
    /// <see cref="IResourceStore.GetPageOutputIntents"/>, and a processor for anything else - a form XObject,
    /// a tiling pattern, a shading, a soft mask - is handed the intents in force where it was invoked.
    /// </summary>
    public class OutputIntentInheritanceTests
    {
        [Fact]
        public void APageProcessorTakesThePagesOwnIntents()
        {
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, "CATALOG", profileObjectNumber: 7));

            var pageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new ArrayToken([Intent(scanner, "PAGE", profileObjectNumber: 8)]) }
            });

            var processor = Build(store, scanner, store.GetPageOutputIntents(pageDictionary));

            Assert.Equal(["PAGE"], Identifiers(processor));
        }

        [Fact]
        public void APageProcessorFallsBackToTheCatalog()
        {
            // A page declaring none sits inside the document's declaration - the fallback GetPageOutputIntents
            // performs, which is why a page factory is the thing that calls it.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, "CATALOG", profileObjectNumber: 7));

            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var processor = Build(store, scanner, store.GetPageOutputIntents(empty));

            Assert.Equal(["CATALOG"], Identifiers(processor));
        }

        [Fact]
        public void ANestedProcessorTakesTheIntentsItIsGiven()
        {
            // A tiling pattern invoked from a page that overrode the catalog paints under the page's intent,
            // not the catalog's, even though the pattern stream is not a page and carries no /OutputIntents.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, "CATALOG", profileObjectNumber: 7));

            var pageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.OutputIntents, new ArrayToken([Intent(scanner, "PAGE", profileObjectNumber: 8)]) }
            });

            var page = Build(store, scanner, store.GetPageOutputIntents(pageDictionary));
            var nested = Build(store, scanner, page.GetCurrentState().OutputIntents);

            Assert.Equal(["PAGE"], Identifiers(nested));
        }

        [Fact]
        public void ANestedProcessorTakesNullAsNoneInEffect()
        {
            // A soft-mask group suppresses the output intent, because its device values are an alpha
            // computation rather than output-device colour. Passing that suppression down is not the same
            // as saying nothing, which is why the processor has no "work it out yourself" path at all.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, "CATALOG", profileObjectNumber: 7));

            var nested = Build(store, scanner, null);

            Assert.Null(nested.GetCurrentState().OutputIntents);
        }

        [Fact]
        public void ANestedProcessorCannotReachForTheCatalog()
        {
            // The trap this shape removes: there is no argument a nested processor can pass that quietly
            // yields the catalog's intents, because it never consults the resource store for them.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, "CATALOG", profileObjectNumber: 7));

            var nested = Build(store, scanner, []);

            Assert.Empty(nested.GetCurrentState().OutputIntents!);
        }

        private static IEnumerable<string?> Identifiers(TestStreamProcessor processor)
            => processor.GetCurrentState().OutputIntents!.Select(x => x.OutputConditionIdentifier);

        private static TestStreamProcessor Build(IResourceStore store, IPdfTokenScanner scanner,
            IReadOnlyList<OutputIntent>? outputIntents)
            => new(store, scanner, outputIntents);

        /// <summary>
        /// The smallest concrete <see cref="BaseStreamProcessor{TPageContent}"/> there can be: these tests
        /// are about which output intents the constructors install, and nothing here is ever processed.
        /// </summary>
        private sealed class TestStreamProcessor : BaseStreamProcessor<object>
        {
            private static readonly CropBox Box = new(new PdfRectangle(0, 0, 100, 100));

            public TestStreamProcessor(IResourceStore resourceStore, IPdfTokenScanner scanner,
                IReadOnlyList<OutputIntent>? outputIntents)
                : base(1, resourceStore, scanner, Parser,
                    new TestFilterProvider(), Box, PdfPig.Geometry.UserSpaceUnit.Default, default,
                    TransformationMatrix.Identity, outputIntents, Options)
            {
            }

            private static IPageContentParser Parser =>
                new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256));

            private static ParsingOptions Options => new() { UseLenientParsing = true, SkipMissingFonts = true };

            public override object Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
                => throw new NotSupportedException();

            public override void RenderGlyph(IFont font, CurrentGraphicsState currentState, double fontSize,
                double pointSize, int code, string unicode, long currentOffset,
                in TransformationMatrix renderingMatrix, in TransformationMatrix textMatrix,
                in TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox)
                => throw new NotSupportedException();

            protected override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
                => throw new NotSupportedException();

            protected override void RenderInlineImage(InlineImage inlineImage)
                => throw new NotSupportedException();

            public override void BeginSubpath() => throw new NotSupportedException();

            public override PdfPoint? CloseSubpath() => throw new NotSupportedException();

            public override void StrokePath(bool close) => throw new NotSupportedException();

            public override void FillPath(FillingRule fillingRule, bool close) => throw new NotSupportedException();

            public override void FillStrokePath(FillingRule fillingRule, bool close) => throw new NotSupportedException();

            public override void MoveTo(double x, double y) => throw new NotSupportedException();

            public override void BezierCurveTo(double x2, double y2, double x3, double y3)
                => throw new NotSupportedException();

            public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
                => throw new NotSupportedException();

            public override void LineTo(double x, double y) => throw new NotSupportedException();

            public override void Rectangle(double x, double y, double width, double height)
                => throw new NotSupportedException();

            public override void EndPath() => throw new NotSupportedException();

            public override void ClosePath() => throw new NotSupportedException();

            public override void ModifyClippingIntersect(FillingRule clippingRule) => throw new NotSupportedException();

            protected override void ClipToRectangle(PdfRectangle rectangle, FillingRule clippingRule)
                => throw new NotSupportedException();

            public override void BeginMarkedContent(NameToken name, NameToken? propertyDictionaryName,
                DictionaryToken? properties) => throw new NotSupportedException();

            public override void EndMarkedContent() => throw new NotSupportedException();

            public override void PaintShading(NameToken shadingName) => throw new NotSupportedException();
        }

        private static ResourceStore BuildStore(TestPdfTokenScanner scanner, DictionaryToken? catalogDictionary)
        {
            return new ResourceStore(
                scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                catalogDictionary,
                new ParsingOptions
                {
                    UseLenientParsing = true,
                    SkipMissingFonts = true,
                    IccProfileService = new TestIccProfileService(4)
                });
        }

        private static DictionaryToken Catalog(TestPdfTokenScanner scanner, string conditionIdentifier,
            long profileObjectNumber)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([Intent(scanner, conditionIdentifier, profileObjectNumber)])
                }
            });
        }

        private static DictionaryToken Intent(TestPdfTokenScanner scanner, string conditionIdentifier,
            long profileObjectNumber)
        {
            var reference = new IndirectReference(profileObjectNumber, 0);

            if (!scanner.Objects.ContainsKey(reference))
            {
                var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.N, new NumericToken(4) },
                    { NameToken.Length, new NumericToken(4) }
                });

                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference,
                    new StreamToken(streamDictionary, [1, 2, 3, 4]));
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create(OutputIntent.PdfXSubtype) },
                { NameToken.OutputConditionIdentifier, new StringToken(conditionIdentifier) },
                { NameToken.DestOutputProfile, new IndirectReferenceToken(reference) }
            });
        }
    }
}
