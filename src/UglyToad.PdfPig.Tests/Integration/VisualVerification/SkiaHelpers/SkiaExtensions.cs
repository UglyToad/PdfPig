namespace UglyToad.PdfPig.Tests.Integration.VisualVerification.SkiaHelpers
{
    using PdfPig.Core;
    using PdfPig.Graphics.Colors;
    using SkiaSharp;

    internal static class SkiaExtensions
    {
        public static SKMatrix ToSKMatrix(this TransformationMatrix transformationMatrix)
        {
            return new SKMatrix((float)transformationMatrix.A, (float)transformationMatrix.C, (float)transformationMatrix.E,
                (float)transformationMatrix.B, (float)transformationMatrix.D, (float)transformationMatrix.F,
                0, 0, 1);
        }

        public static SKColor ToSKColor(this IColor pdfColor, double alpha)
        {
            var color = SKColors.Black;
            if (pdfColor != null && pdfColor is not PatternColor)
            {
                var (r, g, b) = pdfColor.ToRGBValues();

                color = new SKColor(Convert.ToByte(r * 255), Convert.ToByte(g * 255), Convert.ToByte(b * 255));
            }

            return color.WithAlpha(Convert.ToByte(alpha * 255));
        }

        public static SKPath ToSKPath(this IReadOnlyList<PdfSubpath> path)
        {
            var skPath = new SKPath() { FillType = SKPathFillType.EvenOdd };
            foreach (var subpath in path)
            {
                foreach (var c in subpath.Commands)
                {
                    if (c is PdfSubpath.Move move)
                    {
                        skPath.MoveTo((float)move.Location.X, (float)move.Location.Y);
                    }
                    else if (c is PdfSubpath.Line line)
                    {
                        skPath.LineTo((float)line.To.X, (float)line.To.Y);
                    }
                    else if (c is PdfSubpath.CubicBezierCurve curve)
                    {
                        skPath.CubicTo((float)curve.FirstControlPoint.X,
                            (float)curve.FirstControlPoint.Y,
                            (float)curve.SecondControlPoint.X,
                            (float)curve.SecondControlPoint.Y,
                            (float)curve.EndPoint.X,
                            (float)curve.EndPoint.Y);
                    }
                    else if (c is PdfSubpath.QuadraticBezierCurve quadratic)
                    {
                        skPath.QuadTo((float)quadratic.ControlPoint.X,
                            (float)quadratic.ControlPoint.Y,
                            (float)quadratic.EndPoint.X,
                            (float)quadratic.EndPoint.Y);
                    }
                    else if (c is PdfSubpath.Close)
                    {
                        skPath.Close();
                    }
                }
            }

            return skPath;
        }

        public static SKPaintStyle? ToSKPaintStyle(this TextRenderingMode textRenderingMode)
        {
            // This is an approximation
            switch (textRenderingMode)
            {
                case TextRenderingMode.Stroke:
                case TextRenderingMode.StrokeClip:
                    return SKPaintStyle.Stroke;

                case TextRenderingMode.Fill:
                case TextRenderingMode.FillClip:
                    return SKPaintStyle.Fill;

                case TextRenderingMode.FillThenStroke:
                case TextRenderingMode.FillThenStrokeClip:
                    return SKPaintStyle.StrokeAndFill;

                case TextRenderingMode.NeitherClip:
                    return SKPaintStyle.Stroke; // Not correct

                case TextRenderingMode.Neither:
                default:
                    return null;
            }
        }
    }
}
