using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Graphics.Core;
using UglyToad.PdfPig.PdfFonts;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal static class SkiaExtensions
    {
        public static SKFontStyle GetFontStyle(this FontDetails fontDetails)
        {
            if (fontDetails.IsBold && fontDetails.IsItalic)
            {
                return SKFontStyle.BoldItalic;
            }
            else if (fontDetails.IsBold)
            {
                return SKFontStyle.Bold;
            }
            else if (fontDetails.IsItalic)
            {
                return SKFontStyle.Italic;
            }
            return SKFontStyle.Normal;
        }

        public static string GetCleanFontName(this IFont font)
        {
            string fontName = font.Name;
            if (fontName.Length > 7 && fontName[6].Equals('+'))
            {
                string subset = fontName.Substring(0, 6);
                if (subset.Equals(subset.ToUpper()))
                {
                    return fontName.Split('+')[1];
                }
            }

            return fontName;
        }

        public static SKPath PdfPathToGraphicsPath(this PdfPath path, int height)
        {
            var gp = PdfSubpathsToGraphicsPath(path, height);
            gp.FillType = path.FillingRule == FillingRule.NonZeroWinding ? SKPathFillType.Winding : SKPathFillType.EvenOdd;
            return gp;
        }

        public static SKPath PdfSubpathsToGraphicsPath(this IReadOnlyList<PdfSubpath> pdfSubpaths, int height)
        {
            var gp = new SKPath();

            foreach (var subpath in pdfSubpaths)
            {
                foreach (var c in subpath.Commands)
                {
                    if (c is Move move)
                    {
                        gp.MoveTo(move.Location.ToSKPoint(height));
                    }
                    else if (c is Line line)
                    {
                        gp.LineTo(line.To.ToSKPoint(height));
                    }
                    else if (c is BezierCurve curve)
                    {
                        if (curve.StartPoint.Equals(curve.FirstControlPoint))
                        {
                            // Quad curve
                            gp.QuadTo(curve.SecondControlPoint.ToSKPoint(height),
                                curve.EndPoint.ToSKPoint(height));
                        }
                        else
                        {
                            // Cubic curve
                            gp.CubicTo(curve.FirstControlPoint.ToSKPoint(height),
                                curve.SecondControlPoint.ToSKPoint(height),
                                curve.EndPoint.ToSKPoint(height));
                        }
                    }
                    else if (c is Close)
                    {
                        gp.Close();
                    }
                }
            }
            return gp;
        }

        public static SKPoint ToSKPoint(this PdfPoint pdfPoint, int height)
        {
            return new SKPoint((float)(pdfPoint.X), (float)(height - pdfPoint.Y));
        }

        public static SKStrokeJoin ToSKStrokeJoin(this LineJoinStyle lineJoinStyle)
        {
            switch (lineJoinStyle)
            {
                case LineJoinStyle.Bevel:
                    return SKStrokeJoin.Bevel;

                case LineJoinStyle.Miter:
                    return SKStrokeJoin.Miter;

                case LineJoinStyle.Round:
                    return SKStrokeJoin.Round;

                default:
                    throw new NotImplementedException($"Unknown LineJoinStyle '{lineJoinStyle}'.");
            }
        }

        public static SKStrokeCap ToSKStrokeCap(this LineCapStyle lineCapStyle)
        {
            switch (lineCapStyle) // to put in helper
            {
                case LineCapStyle.Butt:
                    return SKStrokeCap.Butt;

                case LineCapStyle.ProjectingSquare:
                    return SKStrokeCap.Square;

                case LineCapStyle.Round:
                    return SKStrokeCap.Round;

                default:
                    throw new NotImplementedException($"Unknown LineCapStyle '{lineCapStyle}'.");
            }
        }

        public static SKPathEffect? ToSKPathEffect(this LineDashPattern lineDashPattern)
        {
            const float oneOver72 = (float)(1.0 / 72.0);

            if (lineDashPattern.Phase != 0 || lineDashPattern.Array?.Count > 0) // to put in helper
            {
                //* https://docs.microsoft.com/en-us/dotnet/api/system.drawing.pen.dashpattern?view=dotnet-plat-ext-3.1
                //* The elements in the dashArray array set the length of each dash and space in the dash pattern. 
                //* The first element sets the length of a dash, the second element sets the length of a space, the
                //* third element sets the length of a dash, and so on. Consequently, each element should be a 
                //* non-zero positive number.

                if (lineDashPattern.Array.Count == 1)
                {
                    List<float> pattern = new List<float>();
                    var v = lineDashPattern.Array[0];
                    pattern.Add((float)v);
                    pattern.Add((float)v);
                    return SKPathEffect.CreateDash(pattern.ToArray(), (float)v); // TODO
                }
                else if (lineDashPattern.Array.Count > 0)
                {
                    List<float> pattern = new List<float>();
                    for (int i = 0; i < lineDashPattern.Array.Count; i++)
                    {
                        var v = lineDashPattern.Array[i];
                        if (v == 0)
                        {
                            pattern.Add(oneOver72);
                        }
                        else
                        {
                            pattern.Add((float)v);
                        }
                    }
                    //pen.DashPattern = pattern.ToArray(); // TODO
                    return SKPathEffect.CreateDash(pattern.ToArray(), pattern[0]); // TODO
                }
                //pen.DashOffset = path.LineDashPattern.Value.Phase; // mult?? //  // TODO
            }
            return null;
        }

        public static SKPathFillType ToSKPathFillType(this FillingRule fillingRule)
        {
            return fillingRule == FillingRule.NonZeroWinding ? SKPathFillType.Winding : SKPathFillType.EvenOdd;
        }

        public static SKColor ToSKColor(this IColor pdfColor, decimal alpha)
        {
            SKColor color = SKColors.Black;
            if (pdfColor != null)
            {
                var colorRgb = pdfColor.ToRGBValues();
                double r = colorRgb.r;
                double g = colorRgb.g;
                double b = colorRgb.b;

                color = new SKColor(Convert.ToByte(r * 255), Convert.ToByte(g * 255), Convert.ToByte(b * 255));
            }
            return color.WithAlpha(Convert.ToByte(alpha * 255));
        }

        public static SKColor GetCurrentNonStrokingColorSKColor(this CurrentGraphicsState currentGraphicsState)
        {
            return currentGraphicsState.CurrentNonStrokingColor.ToSKColor(currentGraphicsState.AlphaConstantNonStroking);
        }

        public static SKColor GetCurrentStrokingColorSKColor(this CurrentGraphicsState currentGraphicsState)
        {
            return currentGraphicsState.CurrentStrokingColor.ToSKColor(currentGraphicsState.AlphaConstantStroking);
        }

        /*
        private static bool doBlending = false;

        public static SKBlendMode ToSKBlendMode(this BlendMode blendMode)
        {
            if (!doBlending)
            {
                return SKBlendMode.SrcOver;
            }

            switch (blendMode)
            {
                // Standard separable blend modes
                case BlendMode.Normal:
                case BlendMode.Compatible:
                    return SKBlendMode.SrcOver; // TODO - Check if correct

                case BlendMode.Multiply:
                    return SKBlendMode.Multiply;

                case BlendMode.Screen:
                    return SKBlendMode.Screen;

                case BlendMode.Overlay:
                    return SKBlendMode.Overlay;

                case BlendMode.Darken:
                    return SKBlendMode.Darken;

                case BlendMode.Lighten:
                    return SKBlendMode.Lighten;

                case BlendMode.ColorDodge:
                    return SKBlendMode.ColorDodge;

                case BlendMode.ColorBurn:
                    return SKBlendMode.ColorBurn;

                case BlendMode.HardLight:
                    return SKBlendMode.HardLight;

                case BlendMode.SoftLight:
                    return SKBlendMode.SoftLight;

                case BlendMode.Difference:
                    return SKBlendMode.Difference;

                case BlendMode.Exclusion:
                    return SKBlendMode.Exclusion;

                // Standard nonseparable blend modes
                case BlendMode.Hue:
                    return SKBlendMode.Hue;

                case BlendMode.Saturation:
                    return SKBlendMode.Saturation;

                case BlendMode.Color:
                    return SKBlendMode.Color;

                case BlendMode.Luminosity:
                    return SKBlendMode.Luminosity;

                default:
                    throw new NotImplementedException($"Cannot convert blend mode '{blendMode}' to SKBlendMode.");
            }
        }
        */

        public static SKBitmap GetSKBitmap(this IPdfImage image)
        {
            var bitmap = SKBitmap.Decode(image.GetImageBytes());

            /*
            if (image.SMask != null)
            {
                byte[] bytesSMask = image.SMask.GetImageBytes();
                using (var bitmapSMask = SKBitmap.Decode(bytesSMask))
                {
                    bitmap.ApplySMask(bitmapSMask);
                    //SKMask mask = SKMask.Create(bitmapSMask.Bytes, bitmapSMask.Info.Rect, (uint)bitmapSMask.RowBytes, SKMaskFormat.A8);
                    //if (!bitmap.InstallMaskPixels(mask))
                    //{
                    //    System.Diagnostics.Debug.WriteLine("Could not install mask pixels.");
                    //}
                }
            }
            */
            return bitmap;
        }

        public static void ApplySMask(this SKBitmap image, SKBitmap smask)
        {
            // What about 'Alpha source' flag?
            SKBitmap scaled;
            if (!image.Info.Rect.Equals(smask.Info.Rect))
            {
                scaled = new SKBitmap(image.Info);
                if (!smask.ScalePixels(scaled, SKFilterQuality.High))
                {
                    // log
                }
            }
            else
            {
                scaled = smask;
            }

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pix = image.GetPixel(x, y);
                    byte alpha = scaled.GetPixel(x, y).Red; // Gray CS (r = g = b)
                    image.SetPixel(x, y, pix.WithAlpha(alpha));
                }
            }
            scaled.Dispose();
        }

        public static byte[] GetImageBytes(this IPdfImage pdfImage)
        {
            if (pdfImage.TryGetPng(out byte[] bytes) && bytes?.Length > 0)
            {
                return bytes;
            }

            if (pdfImage.TryGetBytes(out var bytesL) && bytesL?.Count > 0)
            {
                return bytesL.ToArray();
            }

            return pdfImage.RawBytes.ToArray();
        }
    }
}
