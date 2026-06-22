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
    using PdfPig.Graphics.Operations.PathConstruction;
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
            // The crop box is defined in unrotated default user space; account for the page
            // rotation to get the visible dimensions of the rendering surface.
            var visibleBounds = cropBox.GetVisibleBounds(rotation);
            width = (int)visibleBounds.Width;
            height = (int)visibleBounds.Height;
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
            var textRenderingMode = currentState.FontState.TextRenderingMode;
            if (textRenderingMode == TextRenderingMode.Neither)
            {
                return;
            }

            if (font is IType3Font type3Font)
            {
                ShowType3Glyph(type3Font, code, in renderingMatrix, in textMatrix);
                return;
            }

            // TODO - Check if font is a vector font and Assert if result matches font.TryGetNormalisedPath(...)
            // We should be able to get the glyph path of all fonts that are vector fonts

            if (font.TryGetNormalisedPath(code, out var path))
            {
                var skPath = path.ToSKPath();
                ShowVectorFontGlyph(skPath,
                    currentState.CurrentStrokingColor!,
                    currentState.CurrentNonStrokingColor!,
                    textRenderingMode,
                    renderingMatrix,
                    textMatrix,
                    transformationMatrix);
            }
            else
            {
                // TODO - Just render the bounding box?
            }
        }

        private void ShowType3Glyph(IType3Font font,
            int code,
            in TransformationMatrix renderingMatrix,
            in TransformationMatrix textMatrix)
        {
            if (!font.TryGetCharProc(code, out var charProcStream))
            {
                return;
            }

            var contentBytes = charProcStream.Decode(FilterProvider, PdfScanner);
            var operations = PageContentParser.Parse(PageNumber,
                new MemoryInputBytes(contentBytes), ParsingOptions.Logger);

            PushState();
            try
            {
                // We only consider path based type 3 fonts (some can be images)
                using var path = ExtractVectorPath(operations);
                if (!path.IsEmpty)
                {
                    ModifyCurrentTransformationMatrix(textMatrix);
                    ModifyCurrentTransformationMatrix(renderingMatrix);
                    ModifyCurrentTransformationMatrix(font.GetFontMatrix());

                    var currentState = GetCurrentState();
                    ShowVectorFontGlyph(path,
                        currentState.CurrentStrokingColor,
                        currentState.CurrentNonStrokingColor,
                        currentState.FontState.TextRenderingMode,
                        in TransformationMatrix.Identity,
                        in TransformationMatrix.Identity,
                        GetCurrentState().CurrentTransformationMatrix);
                }
            }
            finally
            {
                PopState();
            }
        }

        private static SKPath ExtractVectorPath(IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var raw = new SKPath { FillType = SKPathFillType.Winding };
            for (int i = 0; i < operations.Count; ++i)
            {
                switch (operations[i])
                {
                    case BeginNewSubpath m:
                        raw.MoveTo((float)m.X, (float)m.Y);
                        break;
                    case AppendStraightLineSegment l:
                        raw.LineTo((float)l.X, (float)l.Y);
                        break;
                    case AppendDualControlPointBezierCurve c:
                        raw.CubicTo((float)c.X1, (float)c.Y1, (float)c.X2, (float)c.Y2,
                            (float)c.X3, (float)c.Y3);
                        break;
                    case AppendStartControlPointBezierCurve v:
                        // 'v' uses the current point as the first control point; SkiaSharp has
                        // no direct equivalent, so emit the cubic with current-point CP1.
                        var lp = raw.LastPoint;
                        raw.CubicTo(lp.X, lp.Y, (float)v.X2, (float)v.Y2, (float)v.X3, (float)v.Y3);
                        break;
                    case AppendEndControlPointBezierCurve y:
                        // 'y' uses (x3,y3) as both the second control point and the end point.
                        raw.CubicTo((float)y.X1, (float)y.Y1, (float)y.X3, (float)y.Y3,
                            (float)y.X3, (float)y.Y3);
                        break;
                    case AppendRectangle r:
                        raw.AddRect(SKRect.Create((float)r.LowerLeftX, (float)r.LowerLeftY,
                            (float)r.Width, (float)r.Height));
                        break;
                    case PdfPig.Graphics.Operations.PathConstruction.CloseSubpath:
                        raw.Close();
                        break;
                }
            }

            return raw;
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
        #endregion
    }
}
