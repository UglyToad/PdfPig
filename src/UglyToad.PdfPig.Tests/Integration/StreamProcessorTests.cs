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
            // The base processor does not render glyphs of layers hidden in the state it is given, while Page
            // always contains everything.
            var file = IntegrationHelpers.GetDocumentPath("GWG150_OptionalContent-OCCD_X4");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                TextOnlyPageInformationFactory.Visibility = document.OptionalContent;
                try
                {
                    var page = document.GetPage(1);
                    var textOnlyPage = document.GetPage<TextOnlyPage>(1);

                    Assert.EndsWith("GWG View 1GWG View 2", page.Text);
                    Assert.DoesNotContain("GWG View 1GWG View 2", textOnlyPage.Text);
                    Assert.True(textOnlyPage.Text.Length < page.Text.Length);
                }
                finally
                {
                    TextOnlyPageInformationFactory.Visibility = null;
                }
            }
        }

        [Fact]
        public void HiddenClipModeGlyphsStillReachRenderGlyph()
        {
            // 8.11.3.1: a hidden glyph in a clip rendering mode still adds to the text clipping path, so the base
            // processor still renders it ('C'), unlike a hidden glyph in a paint-only mode ('F'). The built-in
            // processor, which always processes everything, makes a letter of every glyph.
            var pdf = BuildPdfWithHiddenLayer(
                "BT\n/F1 12 Tf\n10 50 Td\n/OC /oc1 BDC\n7 Tr (C) Tj\n0 Tr (F) Tj\nEMC\n(V) Tj\nET");

            using (var document = PdfDocument.Open(pdf))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                TextOnlyPageInformationFactory.Visibility = document.OptionalContent;
                try
                {
                    Assert.Equal("CV", document.GetPage<TextOnlyPage>(1).Text);
                }
                finally
                {
                    TextOnlyPageInformationFactory.Visibility = null;
                }

                Assert.Equal("CFV", document.GetPage(1).Text);
            }
        }

        [Fact]
        public void TaggingProcessorRecordsTheConditionOfEachGlyph()
        {
            var file = IntegrationHelpers.GetDocumentPath("GWG151_OptionalContent-RBGroup_X4");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                var state = document.OptionalContent;
                Assert.NotNull(state);

                var tags = document.GetPage<TextOnlyPage>(1).Tags;

                // The "Default" layer carries the body text: visible by default.
                var viewOne = state.Groups.Single(g => g.Name == "GWG View 1");
                var viewOneOn = state.WithGroupState(viewOne, true);

                Assert.Contains(tags, t => !t.Condition.IsAlways && t.Condition.IsVisible(state));

                // Glyphs of the "GWG View 1" label: hidden by default, visible with the layer on.
                var viewOneGlyphs = tags.Where(t => !t.Condition.IsVisible(state) && t.Condition.IsVisible(viewOneOn)).ToList();
                Assert.Equal("GWG View 1", string.Concat(viewOneGlyphs.Select(t => t.Unicode)));
            }
        }

        [Fact]
        public void TaggingProcessorResolvesMembershipDictionaries()
        {
            // GWG152: the "GWG View 1" and "GWG View 2" labels are each under an OCMD whose /VE is
            // [/And <Use OCMD> <GWG View n>]; all three groups are OFF by default and the views are a radio group.
            var file = IntegrationHelpers.GetDocumentPath("GWG152_OptionalContent-OCMD_X4");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                var state = document.OptionalContent;
                Assert.NotNull(state);

                var tags = document.GetPage<TextOnlyPage>(1).Tags;

                OptionalContentState With(params string[] names)
                {
                    var result = state;
                    foreach (var name in names)
                    {
                        result = result.WithGroupState(result.Groups.Single(g => g.Name == name), true);
                    }

                    return result;
                }

                string VisibleOnlyIn(OptionalContentState on)
                    => string.Concat(tags.Where(t => !t.Condition.IsVisible(state) && t.Condition.IsVisible(on)).Select(t => t.Unicode));

                Assert.Equal(string.Empty, VisibleOnlyIn(With("GWG View 1")));
                Assert.Equal(string.Empty, VisibleOnlyIn(With("Use OCMD")));
                Assert.Equal("GWG View 1", VisibleOnlyIn(With("Use OCMD", "GWG View 1")));
                Assert.Equal("GWG View 2", VisibleOnlyIn(With("Use OCMD", "GWG View 2")));
            }
        }

        [Fact]
        public void XObjectOptionalContentContributesToTheCondition()
        {
            // 8.11.3.3: a form XObject's /OC governs everything it draws.
            const string form = "BT\n/F1 12 Tf\n10 20 Td\n(B) Tj\nET";
            const string content = "BT\n/F1 12 Tf\n10 50 Td\n(A) Tj\nET\n/Fm1 Do\nBT\n/F1 12 Tf\n10 80 Td\n(C) Tj\nET";
            var pdf = BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R /OCProperties << /OCGs [5 0 R] /D << /OFF [5 0 R] >> >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 6 0 R >> /XObject << /Fm1 7 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /OCG /Name (Hidden) >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                $"<< /Type /XObject /Subtype /Form /BBox [0 0 200 200] /OC 5 0 R /Resources << /Font << /F1 6 0 R >> >> /Length {form.Length} >>\nstream\n{form}\nendstream");

            using (var document = PdfDocument.Open(pdf))
            {
                document.AddPageFactory<TextOnlyPage, TextOnlyPageInformationFactory>();

                var state = document.OptionalContent;
                Assert.NotNull(state);
                var on = state.WithGroupState(state.Groups[0], true);

                var page = document.GetPage<TextOnlyPage>(1);
                Assert.Equal("ABC", page.Text);

                var tags = page.Tags;
                Assert.True(tags[0].Condition.IsAlways);
                Assert.False(tags[1].Condition.IsAlways);
                Assert.False(tags[1].Condition.IsVisible(state));
                Assert.True(tags[1].Condition.IsVisible(on));
                Assert.True(tags[2].Condition.IsAlways);
            }
        }

        /// <summary>
        /// A one-page PDF whose optional content group 'oc1' is OFF in the default configuration, with
        /// Helvetica as /F1.
        /// </summary>
        private static byte[] BuildPdfWithHiddenLayer(string content)
        {
            return BuildPdf(
                "<< /Type /Catalog /Pages 2 0 R /OCProperties << /OCGs [5 0 R] /D << /OFF [5 0 R] >> >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 6 0 R >> /Properties << /oc1 5 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /OCG /Name (Hidden) >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        }

        /// <summary>
        /// A PDF made of <paramref name="objects"/>, numbered from 1; the first is the catalog.
        /// </summary>
        private static byte[] BuildPdf(params string[] objects)
        {
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

            public IReadOnlyList<(string Unicode, OptionalContentCondition Condition)> Tags { get; }

            public TextOnlyPage(int number, string text, IReadOnlyList<(string Unicode, OptionalContentCondition Condition)>? tags = null)
            {
                Number = number;
                Text = text;
                Tags = tags ?? [];
            }
        }

        public readonly struct TextOnlyPageContent
        {
            public IReadOnlyList<string> Letters { get; }

            public IReadOnlyList<(string Unicode, OptionalContentCondition Condition)> Tags { get; }

            public TextOnlyPageContent(IReadOnlyList<string> letters, IReadOnlyList<(string Unicode, OptionalContentCondition Condition)> tags)
            {
                Letters = letters;
                Tags = tags;
            }
        }

        public class TextOnlyPageInformationFactory : BasePageFactory<TextOnlyPage>
        {
            /// <summary>
            /// The state the processors created by this factory evaluate hidden optional content against, or null for none.
            /// </summary>
            [ThreadStatic]
            public static OptionalContentState? Visibility;

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
                    ParsingOptions,
                    Visibility);

                TextOnlyPageContent content = context.Process(pageNumber, operations);

                return new TextOnlyPage(pageNumber, string.Concat(content.Letters), content.Tags);
            }
        }

        public sealed class TextOnlyStreamProcessor : BaseStreamProcessor<TextOnlyPageContent>
        {
            private readonly List<string> _letters = new List<string>();
            private readonly List<(string Unicode, OptionalContentCondition Condition)> _tags = new List<(string Unicode, OptionalContentCondition Condition)>();

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
                ParsingOptions parsingOptions,
                OptionalContentState? visibility)
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
                    parsingOptions,
                    visibility)
            {
            }

            public override TextOnlyPageContent Process(int pageNumberCurrent,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                CloneAllStates();

                ProcessOperations(operations);

                return new TextOnlyPageContent(_letters, _tags);
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
                _tags.Add((unicode, CurrentOptionalContent));
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
