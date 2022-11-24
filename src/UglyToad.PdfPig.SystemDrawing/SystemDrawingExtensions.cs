using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace UglyToad.PdfPig.SystemDrawing
{
    public static class SystemDrawingExtensions
    {
        public static GraphicsPath ToGraphicsPath(this PdfPath path, int height, double scale)
        {
            GraphicsPath gp = new GraphicsPath(path.FillingRule == FillingRule.NonZeroWinding ? FillMode.Winding : FillMode.Alternate);
            foreach (var subpath in path)
            {
                gp.StartFigure();

                foreach (var c in subpath.Commands)
                {
                    if (c is Move move)
                    {
                        // ??
                    }
                    else if (c is Line line)
                    {
                        gp.AddLine(ToPointF(line.From, height, scale), ToPointF(line.To, height, scale));
                    }
                    else if (c is BezierCurve curve)
                    {
                        gp.AddBezier(ToPointF(curve.StartPoint, height, scale),
                                    ToPointF(curve.FirstControlPoint, height, scale),
                                    ToPointF(curve.SecondControlPoint, height, scale),
                                    ToPointF(curve.EndPoint, height, scale));
                    }
                    else if (c is Close)
                    {
                        gp.CloseFigure();
                    }
                }
            }
            return gp;
        }

        public static PointF ToPointF(this PdfPoint pdfPoint, int height, double scale)
        {
            return new PointF((float)(pdfPoint.X * scale), (float)(height - pdfPoint.Y * scale));
        }

        /// <summary>
        /// Default to Black.
        /// </summary>
        /// <param name="pdfColor"></param>
        /// <returns></returns>
        public static Color ToSystemColor(this IColor pdfColor)
        {
            if (pdfColor != null)
            {
                var colorRgb = pdfColor.ToRGBValues();
                if (pdfColor is AlphaColor alphaColor)
                {
                    return Color.FromArgb((int)(alphaColor.A * 255), (int)(colorRgb.r * 255), (int)(colorRgb.g * 255), (int)(colorRgb.b * 255));
                }
                return Color.FromArgb((int)(colorRgb.r * 255), (int)(colorRgb.g * 255), (int)(colorRgb.b * 255));

            }
            return Color.Black;
        }

    }
}
