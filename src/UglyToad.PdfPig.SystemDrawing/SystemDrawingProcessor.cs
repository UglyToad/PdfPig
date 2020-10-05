namespace UglyToad.PdfPig.SystemDrawing
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.PdfFonts;
    using static UglyToad.PdfPig.Core.PdfSubpath;

    /// <inheritdoc/>
    public class SystemDrawingProcessor : BaseDrawingProcessor
    {
        int height;
        int width;
        double mult;
        Graphics graphics;

        public SystemDrawingProcessor() : base(new SystemDrawingLogger())
        { }

        /// <inheritdoc/>
        public override MemoryStream DrawPage(Page page, double scale)
        {
            var ms = new MemoryStream();
            base.Init(page);
            mult = scale;

            height = ToInt(page.Height);
            width = ToInt(page.Width);

            using (var bitmap = new Bitmap(width, height))
            using (graphics = Graphics.FromImage(bitmap))
            {
                UpdateClipPath();

                graphics.FillRectangle(Brushes.White, 0, 0, width, height);

                foreach (var stateOperation in page.Operations)
                {
                    stateOperation.Run(this);
                }

                bitmap.Save(ms, ImageFormat.Png);
            }

            return ms;
        }

        public override void DrawImage(IPdfImage image)
        {
            var upperLeft = image.Bounds.TopLeft.ToPointF(height, mult);
            if (image.Bounds.Rotation != 0)
            {
                var mat = new Matrix();
                mat.RotateAt((float)-image.Bounds.Rotation, upperLeft);
                graphics.MultiplyTransform(mat);
            }

            if (image.TryGetPng(out var png))
            {
                using (var img = Image.FromStream(new MemoryStream(png)))
                {
                    graphics.DrawImage(img, upperLeft.X, upperLeft.Y, (float)(image.Bounds.Width * mult), (float)(image.Bounds.Height * mult));
                }
            }
            else
            {
                if (image.TryGetBytes(out var bytes))
                {
                    try
                    {
                        graphics.DrawImage(Image.FromStream(new MemoryStream(bytes.ToArray())), upperLeft.X, upperLeft.Y, (float)(image.Bounds.Width * mult), (float)(image.Bounds.Height * mult));
                        return;
                    }
                    catch (Exception)
                    {

                    }
                }

                try
                {
                    graphics.DrawImage(Image.FromStream(new MemoryStream(image.RawBytes.ToArray())), upperLeft.X, upperLeft.Y, (float)(image.Bounds.Width * mult), (float)(image.Bounds.Height * mult));
                }
                catch (Exception)
                {
                    RectangleF rect = new RectangleF(upperLeft.X, upperLeft.Y, (float)(image.Bounds.Width * mult), (float)(image.Bounds.Height * mult));
                    graphics.FillRectangle(Brushes.HotPink, rect);
                }
            }
            graphics.ResetTransform();
        }

        public override void DrawLetter(List<PdfSubpath> pdfSubpaths, IColor color, TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix)
        {
            if (pdfSubpaths == null || pdfSubpaths.Count == 0)
            {
                throw new ArgumentException("DrawLetter(): empty path");
            }

            GraphicsPath gp = new GraphicsPath(FillMode.Alternate);
            foreach (var subpath in pdfSubpaths)
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
                        var from = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, line.From);
                        var to = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, line.To);

                        gp.AddLine((float)(from.x * mult), (float)(height - from.y * mult),
                                   (float)(to.x * mult), (float)(height - to.y * mult));
                    }
                    else if (c is BezierCurve curve)
                    {
                        var start = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.StartPoint);
                        var first = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.FirstControlPoint);
                        var second = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.SecondControlPoint);
                        var end = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.EndPoint);

                        gp.AddBezier((float)(start.x * mult), (float)(height - start.y * mult),
                                     (float)(first.x * mult), (float)(height - first.y * mult),
                                     (float)(second.x * mult), (float)(height - second.y * mult),
                                     (float)(end.x * mult), (float)(height - end.y * mult));
                    }
                    else if (c is Close)
                    {
                        gp.CloseFigure();
                    }
                }
            }

            var fillBrush = Brushes.Black;
            if (color != null)
            {
                fillBrush = new SolidBrush(color.ToSystemColor());
            }

            graphics.FillPath(fillBrush, gp);
        }

        public override void DrawLetter(string value, PdfRectangle glyphRectangle, PdfPoint startBaseLine, PdfPoint endBaseLine,
            double width, double fontSize, FontDetails font, IColor color, double pointSize)
        {
            var upperLeft = glyphRectangle.TopLeft.ToPointF(height, mult);

            if (glyphRectangle.Rotation != 0)
            {
                var mat = new Matrix();
                mat.RotateAt((float)-glyphRectangle.Rotation, upperLeft);
                graphics.MultiplyTransform(mat);
            }

            RectangleF rect = new RectangleF(upperLeft.X, upperLeft.Y, (float)(glyphRectangle.Width * mult), (float)(glyphRectangle.Height * mult));
            graphics.FillRectangle(Brushes.Red, rect);
            graphics.DrawRectangle(Pens.Red, upperLeft.X, upperLeft.Y, (float)(glyphRectangle.Width * mult), (float)(glyphRectangle.Height * mult));

            var style = font.IsBold ? FontStyle.Bold : (font.IsItalic ? FontStyle.Italic : FontStyle.Regular);
            Font drawFont = new Font(CleanFontName(font.Name), (float)(pointSize * mult), style, GraphicsUnit.Point);
            graphics.DrawString(value, drawFont, new SolidBrush(color.ToSystemColor()), startBaseLine.ToPointF(height, mult));

            graphics.ResetTransform();
        }

        private static string CleanFontName(string font)
        {
            if (font.Length > 7 && font[6].Equals('+'))
            {
                string subset = font.Substring(0, 6);
                if (subset.Equals(subset.ToUpper()))
                {
                    return font.Split('+')[1];
                }
            }

            return font;
        }

        public override void DrawPath(PdfPath path)
        {
            var gp = path.ToGraphicsPath(height, mult);

            if (path.IsFilled)
            {
                graphics.FillPath(new SolidBrush(path.FillColor.ToSystemColor()), gp);
            }

            if (path.IsStroked)
            {
                var lineWidth = (float)((double)path.LineWidth * mult);

                var pen = new Pen(path.StrokeColor.ToSystemColor(), lineWidth);
                switch (path.LineJoinStyle)
                {
                    case PdfPig.Graphics.Core.LineJoinStyle.Bevel:
                        pen.LineJoin = LineJoin.Bevel;
                        break;

                    case PdfPig.Graphics.Core.LineJoinStyle.Miter:
                        pen.LineJoin = LineJoin.Miter;
                        break;

                    case PdfPig.Graphics.Core.LineJoinStyle.Round:
                        pen.LineJoin = LineJoin.Round;
                        break;
                }

                switch (path.LineCapStyle)
                {
                    case PdfPig.Graphics.Core.LineCapStyle.Butt:
                        pen.StartCap = LineCap.Flat; // ????
                        pen.EndCap = LineCap.Flat; // ????
                        //pen.DashCap = DashCap.Flat; // ????
                        break;

                    case PdfPig.Graphics.Core.LineCapStyle.ProjectingSquare:
                        pen.StartCap = LineCap.Square; // ????
                        pen.EndCap = LineCap.Square; // ????
                        //pen.DashCap = DashCap.Triangle; // ????
                        break;

                    case PdfPig.Graphics.Core.LineCapStyle.Round:
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        pen.DashCap = DashCap.Round;
                        break;
                }

                if (path.LineDashPattern.HasValue)
                {
                    /*
                     * https://docs.microsoft.com/en-us/dotnet/api/system.drawing.pen.dashpattern?view=dotnet-plat-ext-3.1
                     * The elements in the dashArray array set the length of each dash and space in the dash pattern. 
                     * The first element sets the length of a dash, the second element sets the length of a space, the
                     * third element sets the length of a dash, and so on. Consequently, each element should be a 
                     * non-zero positive number.
                     */
                    if (path.LineDashPattern.Value.Array.Count == 1)
                    {
                        List<float> pattern = new List<float>();
                        var v = path.LineDashPattern.Value.Array[0];
                        pattern.Add((float)((double)v / mult));
                        pattern.Add((float)((double)v / mult));
                        pen.DashPattern = pattern.ToArray();
                    }
                    else if (path.LineDashPattern.Value.Array.Count > 0)
                    {
                        List<float> pattern = new List<float>();
                        for (int i = 0; i < path.LineDashPattern.Value.Array.Count; i++)
                        {
                            var v = path.LineDashPattern.Value.Array[i];
                            if (v == 0)
                            {
                                pattern.Add((float)(1.0 / 72.0 * mult));
                            }
                            else
                            {
                                pattern.Add((float)((double)v / mult));
                            }
                        }
                        pen.DashPattern = pattern.ToArray();
                    }

                    pen.DashOffset = path.LineDashPattern.Value.Phase; // mult??
                }

                graphics.DrawPath(pen, gp);
            }
        }
        private int ToInt(double value)
        {
            return (int)Math.Ceiling(value * mult);
        }

        public override void UpdateClipPath()
        {
            var clipping = GetCurrentState().CurrentClippingPath.ToGraphicsPath(height, mult);
            //graphics.ResetClip();
            graphics.SetClip(clipping, CombineMode.Replace);
        }
    }
}
