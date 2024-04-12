namespace UglyToad.PdfPig.Tests.Integration.VisualVerification.SkiaHelpers
{
    using Content;
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
    using SkiaSharp;

    internal sealed class SkiaGlyphStreamProcessor : BaseStreamProcessor<SKPicture>
    {
        private readonly SKMatrix yAxisFlipMatrix;
        private readonly int height;
        private readonly int width;

        private SKCanvas canvas;

        public SkiaGlyphStreamProcessor(int pageNumber, IResourceStore resourceStore, IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser, ILookupFilterProvider filterProvider, CropBox cropBox,
            UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation, TransformationMatrix initialMatrix,
            ParsingOptions parsingOptions)
            : base(pageNumber, resourceStore, pdfScanner, pageContentParser,
            filterProvider, cropBox, userSpaceUnit, rotation, initialMatrix,
            parsingOptions)
        {
            width = (int)cropBox.Bounds.Width;
            height = (int)cropBox.Bounds.Height;
            yAxisFlipMatrix = SKMatrix.CreateScale(1, -1, 0, height / 2f);
        }

        public override SKPicture Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            CloneAllStates();

            using (var recorder = new SKPictureRecorder())
            using (canvas = recorder.BeginRecording(SKRect.Create(width, height)))
            {
                canvas.Clear(SKColors.White);
                ProcessOperations(operations);
                canvas.Flush();
                return recorder.EndRecording();
            }
        }

        public override void PopState()
        {
            base.PopState();
            canvas!.Restore();
        }

        public override void PushState()
        {
            base.PushState();
            canvas!.Save();
        }

        public override void RenderGlyph(IFont font,
            IColor strokingColor,
            IColor nonStrokingColor,
            TextRenderingMode textRenderingMode,
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
            if (textRenderingMode == TextRenderingMode.Neither)
            {
                return;
            }

            // TODO - Check if font is a vector font and Assert if result matches font.TryGetNormalisedPath(...)
            // We should be able to get the glyph path of all fonts that are vector fonts

            if (font.TryGetNormalisedPath(code, out var path))
            {
                var skPath = path.ToSKPath();
                ShowVectorFontGlyph(skPath, strokingColor, nonStrokingColor, textRenderingMode, renderingMatrix,
                    textMatrix, transformationMatrix);
            }
            else
            {
                // TODO - Just render the bounding box?
            }
        }

        private void ShowVectorFontGlyph(SKPath path,
            IColor strokingColor,
            IColor nonStrokingColor,
            TextRenderingMode textRenderingMode,
            in TransformationMatrix renderingMatrix,
            in TransformationMatrix textMatrix,
            in TransformationMatrix transformationMatrix)
        {
            var transformMatrix = renderingMatrix.ToSKMatrix()
                .PostConcat(textMatrix.ToSKMatrix())
                .PostConcat(transformationMatrix.ToSKMatrix())
                .PostConcat(yAxisFlipMatrix);

            var style = textRenderingMode.ToSKPaintStyle();
            if (!style.HasValue)
            {
                return;
            }

            var color = style == SKPaintStyle.Stroke ? strokingColor : nonStrokingColor;

            using (var transformedPath = new SKPath())
            using (var fillBrush = new SKPaint())
            {
                fillBrush.Style = style.Value;
                fillBrush.Color = color.ToSKColor(GetCurrentState().AlphaConstantNonStroking);
                fillBrush.IsAntialias = true;
                path.Transform(transformMatrix, transformedPath);
                canvas!.DrawPath(transformedPath, fillBrush);
            }
        }

        #region No op
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
            return null;
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

        public override void BeginMarkedContent(NameToken name, NameToken propertyDictionaryName, DictionaryToken properties)
        {
            // No op
        }

        public override void EndMarkedContent()
        {
            // No op
        }

        public override void BezierCurveTo(double x2, double y2, double x3, double y3)
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
        #endregion
    }
}
