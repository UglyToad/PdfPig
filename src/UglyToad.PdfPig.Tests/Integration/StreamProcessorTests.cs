namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Text;
    using Content;
    using Outline.Destinations;
    using PdfFonts;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors.Icc;
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

        [Fact]
        public void TextOnlySkipsHiddenOptionalContent()
        {
            // The base processor does not render glyphs of hidden layers, so a custom processor
            // that does not check for optional content itself gets the same text as the built-in one.
            var file = IntegrationHelpers.GetDocumentPath("GWG150_OptionalContent-OCCD_X4");

            using (var document = PdfDocument.Open(file, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                var page = document.GetPage(1);
                var textOnlyPage = document.GetPage<TextOnlyPage>(1);

                Assert.DoesNotContain("GWG View 1GWG View 2", textOnlyPage.Text);
                Assert.Equal(string.Concat(page.Letters.Select(l => l.Value)), textOnlyPage.Text);
            }
        }

        [Fact]
        public void HiddenClipModeGlyphsStillReachRenderGlyph()
        {
            // 8.11.3.1: a hidden glyph in a clip rendering mode still adds to the text clipping path, so the base
            // processor still renders it ('C'), unlike a hidden glyph in a paint-only mode ('F'). The built-in
            // processor makes no letter of it, as it does not appear on the page.
            var pdf = BuildPdfWithHiddenLayer(
                "BT\n/F1 12 Tf\n10 50 Td\n/OC /oc1 BDC\n7 Tr (C) Tj\n0 Tr (F) Tj\nEMC\n(V) Tj\nET");

            using (var document = PdfDocument.Open(pdf, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                Assert.Equal("CV", document.GetPage<TextOnlyPage>(1).Text);
                Assert.Equal("V", document.GetPage(1).Text);
            }

            using (var document = PdfDocument.Open(pdf))
            {
                Assert.Equal("CFV", document.GetPage(1).Text);
            }
        }

        /// <summary>
        /// A one-page PDF whose optional content group 'oc1' is OFF in the default configuration, with
        /// Helvetica as /F1.
        /// </summary>
        private static byte[] BuildPdfWithHiddenLayer(string content)
        {
            string[] objects =
            [
                "<< /Type /Catalog /Pages 2 0 R /OCProperties << /OCGs [5 0 R] /D << /OFF [5 0 R] >> >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 6 0 R >> /Properties << /oc1 5 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /OCG /Name (Hidden) >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
            ];

            var sb = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (int i = 0; i < objects.Length; i++)
            {
                offsets.Add(sb.Length);
                sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            int xref = sb.Length;
            sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (int offset in offsets)
            {
                sb.Append($"{offset:D10} 00000 n \n");
            }

            sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            return Encoding.ASCII.GetBytes(sb.ToString());
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
                    ResourceStore.GetPageOutputIntentProfile(dictionary),
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
                IIccProfile? outputIntentProfile,
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
                    outputIntentProfile,
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

            public override void ModifyClippingIntersect(FillingRule clippingRule)
            {
                // No op
            }

            protected override void ClipToRectangle(PdfRectangle rectangle, FillingRule clippingRule)
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
