using System.Drawing;
using System.Drawing.Drawing2D;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Graphics.Core;
using UglyToad.PdfPig.Graphics.Shading;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace UglyToad.PdfPig.SystemDrawing
{
    public static class SystemDrawingExtensions
    {
        /// <summary>
        /// To implement, using a placeholder for the moment
        /// </summary>
        public static Brush ToSystemGradientBrush(this PdfShading pdfShading)
        {
            return new LinearGradientBrush(new PointF(0, 0), new PointF(1, 1), Color.Green, Color.Red);
        }

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
                return Color.FromArgb((int)(colorRgb.r * 255), (int)(colorRgb.g * 255), (int)(colorRgb.b * 255));
            }
            return Color.Black;
        }

        public static FillMode ToSystemFillMode(this FillingRule fillingRule)
        {
            switch (fillingRule)
            {
                case FillingRule.NonZeroWinding:
                    return FillMode.Winding;

                case FillingRule.EvenOdd:
                default:
                    return FillMode.Alternate;
            }
        }

        public static LineJoin ToSystemLineJoin(this LineJoinStyle lineJoinStyle)
        {
            switch (lineJoinStyle)
            {
                case LineJoinStyle.Bevel:
                    return LineJoin.Bevel;

                case LineJoinStyle.Miter:
                    return LineJoin.Bevel;

                default:
                case LineJoinStyle.Round:
                    return LineJoin.Round;
            }
        }

        public static LineCap ToSystemLineCap(this LineCapStyle lineCapStyle)
        {
            switch(lineCapStyle)
            {
                default:
                case LineCapStyle.Butt:
                    return LineCap.Square; //????

                case LineCapStyle.ProjectingSquare:
                    return LineCap.DiamondAnchor; // ?????????

                case LineCapStyle.Round:
                    return LineCap.Round;
            }
        }

        public static DashCap ToSystemDashCap(this LineCapStyle lineCapStyle)
        {
            switch (lineCapStyle)
            {
                default:
                case LineCapStyle.Butt:
                    return DashCap.Flat; //????

                case LineCapStyle.ProjectingSquare:
                    return DashCap.Triangle; // ?????????

                case LineCapStyle.Round:
                    return DashCap.Round;
            }
        }
    }
}
