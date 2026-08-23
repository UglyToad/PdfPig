namespace UglyToad.PdfPig.Tests.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
    /// Which output intent profile a stream processor starts with (14.11.5). The processor is told; it
    /// never works it out. A page factory resolves the profile with
    /// <see cref="IResourceStore.GetPageOutputIntentProfile"/>, and a processor for anything else - a form
    /// XObject, a tiling pattern, a shading, a soft mask - is handed the profile in force where it was
    /// invoked.
    /// </summary>
    public class OutputIntentInheritanceTests
    {
        [Fact]
        public void APageProcessorTakesThePagesOwnProfile()
        {
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1));

            var pageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([Intent(scanner, profileObjectNumber: 8, profileId: 2)])
                }
            });

            var processor = Build(store, scanner, store.GetPageOutputIntentProfile(pageDictionary));

            Assert.Equal(2, IdOf(processor));
        }

        [Fact]
        public void APageProcessorFallsBackToTheCatalog()
        {
            // A page declaring none sits inside the document's declaration - the fallback the resource store
            // performs, which is why it is the thing that owns this question.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1));

            var empty = new DictionaryToken(new Dictionary<NameToken, IToken>());
            var processor = Build(store, scanner, store.GetPageOutputIntentProfile(empty));

            Assert.Equal(1, IdOf(processor));
        }

        [Fact]
        public void AServiceThatDoesNotOptInLeavesNoProfileToManageThrough()
        {
            // 14.11.5 makes honouring an output intent the consumer's choice, and the choice is now made
            // once per page rather than at every colour operator.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1),
                useOutputIntent: false);

            var processor = Build(store, scanner, store.GetPageOutputIntentProfile(null));

            Assert.Null(processor.GetCurrentState().OutputIntentProfile);
        }

        [Fact]
        public void ANestedProcessorTakesTheProfileItIsGiven()
        {
            // A tiling pattern invoked from a page that overrode the catalog paints under the page's
            // profile, not the catalog's, even though the pattern stream is not a page and carries no
            // /OutputIntents of its own.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1));

            var pageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([Intent(scanner, profileObjectNumber: 8, profileId: 2)])
                }
            });

            var page = Build(store, scanner, store.GetPageOutputIntentProfile(pageDictionary));
            var nested = Build(store, scanner, page.GetCurrentState().OutputIntentProfile);

            Assert.Equal(2, IdOf(nested));
        }

        [Fact]
        public void ANestedProcessorTakesNullAsNoManagement()
        {
            // A soft-mask group suppresses colour management, because its device values are an alpha
            // computation rather than output-device colour. Passing that suppression down is not the same
            // as saying nothing, which is why the processor has no "work it out yourself" path at all.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1));

            var nested = Build(store, scanner, null);

            Assert.Null(nested.GetCurrentState().OutputIntentProfile);
        }

        [Fact]
        public void TheProfileSurvivesAGraphicsStateClone()
        {
            // q/Q must not lose it: the profile is part of the state, like the rendering intent it pairs with.
            var scanner = new TestPdfTokenScanner();
            var store = BuildStore(scanner, Catalog(scanner, profileObjectNumber: 7, profileId: 1));

            var processor = Build(store, scanner, store.GetPageOutputIntentProfile(null));
            var clone = processor.GetCurrentState().DeepClone();

            Assert.Equal(1, ((IdProfile)clone.OutputIntentProfile!).Id);
        }

        private static int IdOf(TestStreamProcessor processor)
            => ((IdProfile)processor.GetCurrentState().OutputIntentProfile!).Id;

        private static TestStreamProcessor Build(IResourceStore store, IPdfTokenScanner scanner,
            IIccProfile? outputIntentProfile)
            => new(store, scanner, outputIntentProfile);

        /// <summary>
        /// A profile that can be told apart from another, so a test can say <i>which</i> one reached the
        /// graphics state rather than only that something did.
        /// </summary>
        private sealed class IdProfile(int id) : IIccProfile
        {
            public int Id { get; } = id;

            public int NumberOfComponents => 4;

            public IReadOnlyList<double> ComponentRanges { get; } = new double[8];

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                transform = null;
                return false;
            }
        }

        /// <summary>
        /// Reads the first byte of the profile stream as its identity.
        /// </summary>
        private sealed class IdProfileService(bool useOutputIntent) : IIccProfileService
        {
            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = new IdProfile(profileBytes.Span[0]);
                return true;
            }

            public bool UseOutputIntent { get; } = useOutputIntent;

            public string? PreferredOutputIntentSubtype => null;
        }

        /// <summary>
        /// The smallest concrete <see cref="BaseStreamProcessor{TPageContent}"/> there can be: these tests
        /// are about what the constructor installs, and nothing here is ever processed.
        /// </summary>
        private sealed class TestStreamProcessor : BaseStreamProcessor<object>
        {
            private static readonly CropBox Box = new(new PdfRectangle(0, 0, 100, 100));

            public TestStreamProcessor(IResourceStore resourceStore, IPdfTokenScanner scanner,
                IIccProfile? outputIntentProfile)
                : base(1, resourceStore, scanner, Parser,
                    new TestFilterProvider(), Box, PdfPig.Geometry.UserSpaceUnit.Default, default,
                    TransformationMatrix.Identity, outputIntentProfile, Options)
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

        private static ResourceStore BuildStore(TestPdfTokenScanner scanner, DictionaryToken? catalogDictionary,
            bool useOutputIntent = true)
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
                    IccProfileService = new IdProfileService(useOutputIntent)
                });
        }

        private static DictionaryToken Catalog(TestPdfTokenScanner scanner, long profileObjectNumber, byte profileId)
        {
            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.OutputIntents,
                    new ArrayToken([Intent(scanner, profileObjectNumber, profileId)])
                }
            });
        }

        private static DictionaryToken Intent(TestPdfTokenScanner scanner, long profileObjectNumber, byte profileId)
        {
            var reference = new IndirectReference(profileObjectNumber, 0);

            if (!scanner.Objects.ContainsKey(reference))
            {
                var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.N, new NumericToken(4) },
                    { NameToken.Length, new NumericToken(1) }
                });

                scanner.Objects[reference] = new ObjectToken(XrefLocation.File(0), reference,
                    new StreamToken(streamDictionary, [profileId]));
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.S, NameToken.Create(OutputIntent.PdfXSubtype) },
                { NameToken.OutputConditionIdentifier, new StringToken($"PROFILE-{profileId}") },
                { NameToken.DestOutputProfile, new IndirectReferenceToken(reference) }
            });
        }
    }
}
