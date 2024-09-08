namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;
    using Outline.Destinations;
    using PdfFonts;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    public class StreamProcessorTests
    {
        [Fact]
        public void TextOnly()
        {
            var file = IntegrationHelpers.GetDocumentPath("cat-genetics");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                var page = document.GetPage(1);
                var textOnlyPage = document.GetPage<TextOnlyPage>(1);

                string expected = string.Concat(page.Letters.Select(l => l.Value));
                Assert.Equal(expected, textOnlyPage.Text);
            }
        }

        #region AdvancedPage

        public readonly struct TextOnlyPage
        {
            public int Number { get; }

            public string Text { get; }

            public TextOnlyPage(int number, string text)
            {
                Number = number;
                Text = text;
            }
        }

        public readonly struct TextOnlyPageContent
        {
            public IReadOnlyList<string> Letters { get; }

            public TextOnlyPageContent(IReadOnlyList<string> letters)
            {
                Letters = letters;
            }
        }

        public class TextOnlyPageInformationFactory : BasePageFactory<TextOnlyPage>
        {
            public TextOnlyPageInformationFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
                : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
            {
            }

            protected override TextOnlyPage ProcessPage(int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                if (operations.Count == 0)
                {
                    return new TextOnlyPage(pageNumber, string.Empty);
                }

                var context = new TextOnlyStreamProcessor(
                    pageNumber,
                    ResourceStore,
                    PdfScanner,
                    PageContentParser,
                    FilterProvider,
                    cropBox,
                    userSpaceUnit,
                    rotation,
                    initialMatrix,
                    ParsingOptions);

                TextOnlyPageContent content = context.Process(pageNumber, operations);

                return new TextOnlyPage(pageNumber, string.Concat(content.Letters));
            }
        }

        public sealed class TextOnlyStreamProcessor : BaseStreamProcessor<TextOnlyPageContent>
        {
            private readonly List<string> _letters = new List<string>();

            public TextOnlyStreamProcessor(int pageNumber,
                IResourceStore resourceStore,
                IPdfTokenScanner pdfScanner,
                IPageContentParser pageContentParser,
                ILookupFilterProvider filterProvider,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                ParsingOptions parsingOptions)
                : base(pageNumber,
                    resourceStore,
                    pdfScanner,
                    pageContentParser,
                    filterProvider,
                    cropBox,
                    userSpaceUnit,
                    rotation,
                    initialMatrix,
                    parsingOptions)
            {
            }

            public override TextOnlyPageContent Process(int pageNumberCurrent,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                CloneAllStates();

                ProcessOperations(operations);

                return new TextOnlyPageContent(_letters);
            }

            public override void RenderGlyph(IFont font,
                CurrentGraphicsState currentState,
                double fontSize,
                double pointSize,
                int code,
                string unicode,
                long currentOffset,
                in TransformationMatrix renderingMatrix,
                in TransformationMatrix textMatrix,
                in TransformationMatrix transformationMatrix,
                CharacterBoundingBox characterBoundingBox)
            {
                _letters.Add(unicode);
            }

            protected override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
            {
                // No op
            }

            public override void BeginSubpath()
            {
                // No op
            }

            public override PdfPoint? CloseSubpath()
            {
                return new PdfPoint();
            }

            public override void StrokePath(bool close)
            {
                // No op
            }

            public override void FillPath(FillingRule fillingRule, bool close)
            {
                // No op
            }

            public override void FillStrokePath(FillingRule fillingRule, bool close)
            {
                // No op
            }

            public override void MoveTo(double x, double y)
            {
                // No op
            }

            public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
            {
                // No op
            }

            public override void LineTo(double x, double y)
            {
                // No op
            }

            public override void Rectangle(double x, double y, double width, double height)
            {
                // No op
            }

            public override void EndPath()
            {
                // No op
            }

            public override void ClosePath()
            {
                // No op
            }

            public override void BeginMarkedContent(NameToken name,
                NameToken propertyDictionaryName,
                DictionaryToken properties)
            {
                // No op
            }

            public override void EndMarkedContent()
            {
                // No op
            }

            public override void ModifyClippingIntersect(FillingRule clippingRule)
            {
                // No op
            }

            public override void PaintShading(NameToken shadingName)
            {
                // No op
            }

            protected override void RenderInlineImage(InlineImage inlineImage)
            {
                // No op
            }

            public override void BezierCurveTo(double x2, double y2, double x3, double y3)
            {
                // No op
            }
        }

        #endregion
    }
}
