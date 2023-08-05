using SkiaSharp;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal partial class SkiaStreamProcessor
    {
        private SKPath? CurrentPath { get; set; }

        public override void BeginSubpath()
        {
            CurrentPath ??= new SKPath();
        }

        public override PdfPoint? CloseSubpath()
        {
            if (CurrentPath == null)
            {
                return null;
            }

            CurrentPath.Close();
            return null;
        }

        public override void MoveTo(double x, double y)
        {
            BeginSubpath();

            if (CurrentPath == null)
            {
                return;
            }

            var point = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            float xs = (float)point.X;
            float ys = (float)(height - point.Y);

            CurrentPath.MoveTo(xs, ys);
        }

        public override void LineTo(double x, double y)
        {
            if (CurrentPath == null)
            {
                return;
            }

            var point = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            float xs = (float)point.X;
            float ys = (float)(height - point.Y);

            CurrentPath.LineTo(xs, ys);
        }

        public override void BezierCurveTo(double x2, double y2, double x3, double y3)
        {
            if (CurrentPath == null)
            {
                return;
            }

            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));
            float x2s = (float)controlPoint2.X;
            float y2s = (float)(height - controlPoint2.Y);
            float x3s = (float)end.X;
            float y3s = (float)(height - end.Y);

            CurrentPath.QuadTo(x2s, y2s, x3s, y3s);
        }

        public override void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (CurrentPath == null)
            {
                return;
            }

            var controlPoint1 = CurrentTransformationMatrix.Transform(new PdfPoint(x1, y1));
            var controlPoint2 = CurrentTransformationMatrix.Transform(new PdfPoint(x2, y2));
            var end = CurrentTransformationMatrix.Transform(new PdfPoint(x3, y3));
            float x1s = (float)controlPoint1.X;
            float y1s = (float)(height - controlPoint1.Y);
            float x2s = (float)controlPoint2.X;
            float y2s = (float)(height - controlPoint2.Y);
            float x3s = (float)end.X;
            float y3s = (float)(height - end.Y);

            CurrentPath.CubicTo(x1s, y1s, x2s, y2s, x3s, y3s);
        }

        public override void ClosePath()
        {
            // TODO - to check, does nothing
        }

        public override void EndPath()
        {
            if (CurrentPath == null)
            {
                return;
            }

            // TODO
            CurrentPath.Dispose();
            CurrentPath = null;
        }

        public override void Rectangle(double x, double y, double width, double height)
        {
            BeginSubpath();

            if (CurrentPath == null)
            {
                return;
            }

            var lowerLeft = CurrentTransformationMatrix.Transform(new PdfPoint(x, y));
            var upperRight = CurrentTransformationMatrix.Transform(new PdfPoint(x + width, y + height));
            float left = (float)lowerLeft.X;
            float top = (float)(this.height - upperRight.Y);
            float right = (float)upperRight.X;
            float bottom = (float)(this.height - lowerLeft.Y);
            SKRect rect = new SKRect(left, top, right, bottom);
            CurrentPath.AddRect(rect);
        }

        private float GetScaledLineWidth()
        {
            var currentState = GetCurrentState();
            // https://stackoverflow.com/questions/25690496/how-does-pdf-line-width-interact-with-the-ctm-in-both-horizontal-and-vertical-di
            // TODO - a hack but works, to put in ContentStreamProcessor
            return (float)(currentState.LineWidth * (decimal)currentState.CurrentTransformationMatrix.A);
        }

        public override void StrokePath(bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.Close();
            }

            var currentState = GetCurrentState();

            PaintStrokePath(currentState);

            CurrentPath.Dispose();
            CurrentPath = null;
        }

        private void PaintStrokePath(CurrentGraphicsState currentGraphicsState)
        {
            if (currentGraphicsState.CurrentStrokingColor?.ColorSpace == ColorSpace.Pattern)
            {
                if (!(currentGraphicsState.CurrentStrokingColor is PatternColor pattern))
                {
                    throw new ArgumentNullException("TODO");
                }

                switch (pattern.PatternType)
                {
                    case PatternType.Tiling:
                        RenderTilingPattern(pattern as TilingPatternColor, true);
                        break;

                    case PatternType.Shading:
                        this.RenderShadingPattern(pattern as ShadingPatternColor, true);
                        break;
                }
            }
            else
            {
                using (var paint = new SKPaint()
                {
                    IsAntialias = antiAliasing,
                    Color = currentGraphicsState.GetCurrentStrokingColorSKColor(),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = Math.Max((float)0.5, GetScaledLineWidth()), // A guess
                    StrokeJoin = currentGraphicsState.JoinStyle.ToSKStrokeJoin(),
                    StrokeCap = currentGraphicsState.CapStyle.ToSKStrokeCap(),
                    PathEffect = currentGraphicsState.LineDashPattern.ToSKPathEffect()
                })
                {
                    //paint.BlendMode = currentGraphicsState.BlendMode.ToSKBlendMode();
                    canvas.DrawPath(CurrentPath, paint);
                }
            }
        }

        public override void FillPath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.Close();
            }

            var currentState = GetCurrentState();

            PaintFillPath(currentState, fillingRule);

            CurrentPath.Dispose();
            CurrentPath = null;
        }

        private void PaintFillPath(CurrentGraphicsState currentGraphicsState, FillingRule fillingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.FillType = fillingRule.ToSKPathFillType();

            if (currentGraphicsState.CurrentNonStrokingColor?.ColorSpace == ColorSpace.Pattern)
            {
                if (!(currentGraphicsState.CurrentNonStrokingColor is PatternColor pattern))
                {
                    throw new ArgumentNullException("TODO");
                }

                switch (pattern.PatternType)
                {
                    case PatternType.Tiling:
                        RenderTilingPattern(pattern as TilingPatternColor, false);
                        break;

                    case PatternType.Shading:
                        this.RenderShadingPattern(pattern as ShadingPatternColor, false);
                        break;
                }
            }
            else
            {
                using (SKPaint paint = new SKPaint()
                {
                    IsAntialias = antiAliasing,
                    Color = currentGraphicsState.GetCurrentNonStrokingColorSKColor(),
                    Style = SKPaintStyle.Fill
                })
                {
                    //paint.BlendMode = currentGraphicsState.BlendMode.ToSKBlendMode();
                    canvas.DrawPath(CurrentPath, paint);
                }
            }
        }

        public override void FillStrokePath(FillingRule fillingRule, bool close)
        {
            if (CurrentPath == null)
            {
                return;
            }

            if (close)
            {
                CurrentPath.Close();
            }

            var currentState = GetCurrentState();

            PaintFillPath(currentState, fillingRule);
            PaintStrokePath(currentState);

            CurrentPath.Dispose();
            CurrentPath = null;
        }

        private void RenderTilingPattern(TilingPatternColor pattern, bool isStroke)
        {
            System.Diagnostics.Debug.WriteLine($"WARNING: Tiling Shader not implemented");
            DebugDrawRect(CurrentPath.Bounds);
            //throw new NotImplementedException("PaintStrokePath Tiling Shader");
        }
    }
}
