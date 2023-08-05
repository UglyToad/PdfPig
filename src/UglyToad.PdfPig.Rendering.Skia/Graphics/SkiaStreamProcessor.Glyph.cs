using SkiaSharp;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.PdfFonts;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal partial class SkiaStreamProcessor
    {
        public override void RenderGlyph(IFont font, IColor strokingColor, IColor nonStrokingColor, TextRenderingMode textRenderingMode, double fontSize, double pointSize, int code, string unicode, long currentOffset,
            TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix, CharacterBoundingBox characterBoundingBox)
        {
            // TODO - update with strokingColor, nonStrokingColor, and textRenderingMode

            IColor color = textRenderingMode == TextRenderingMode.Fill ? nonStrokingColor : strokingColor;

            try
            {
                if (font.TryGetNormalisedPath(code, out var path))
                {
                    ShowVectorFontGlyph(path, color, renderingMatrix, textMatrix, transformationMatrix);
                }
                else
                {
                    // VCarefull here sone controle char have a representation - need testing, see issue 655
                    if (!CanRender(unicode))
                    {
                        return;
                    }

                    ShowNonVectorFontGlyph(font, color, pointSize, unicode, renderingMatrix, textMatrix, transformationMatrix, characterBoundingBox);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowGlyph: {ex}");
            }
        }

        private void ShowVectorFontGlyph(IReadOnlyList<PdfSubpath> path, IColor color,
            TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix)
        {
            // Vector based font
            using (var gp = new SKPath() { FillType = SKPathFillType.EvenOdd })
            {
                foreach (var subpath in path)
                {
                    foreach (var c in subpath.Commands)
                    {
                        if (c is Move move)
                        {
                            var (x, y) = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, move.Location);
                            gp.MoveTo((float)x, (float)(height - y));
                        }
                        else if (c is Line line)
                        {
                            var (x, y) = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, line.To);
                            gp.LineTo((float)x, (float)(height - y));
                        }
                        else if (c is BezierCurve curve)
                        {
                            if (curve.StartPoint.Equals(curve.FirstControlPoint))
                            {
                                // Quad curve
                                var second = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.SecondControlPoint);
                                var end = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.EndPoint);
                                gp.QuadTo((float)(second.x), (float)(height - second.y),
                                          (float)(end.x), (float)(height - end.y));
                            }
                            else
                            {
                                // Cubic curve
                                var first = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.FirstControlPoint);
                                var second = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.SecondControlPoint);
                                var end = TransformPoint(renderingMatrix, textMatrix, transformationMatrix, curve.EndPoint);
                                gp.CubicTo((float)first.x, (float)(height - first.y),
                                           (float)second.x, (float)(height - second.y),
                                           (float)end.x, (float)(height - end.y));
                            }
                        }
                        else if (c is Close)
                        {
                            gp.Close();
                        }
                    }
                }

                using (var fillBrush = new SKPaint()
                {
                    Style = SKPaintStyle.Fill,
                    //BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(), // TODO - check if correct
                    Color = SKColors.Black,
                    IsAntialias = antiAliasing
                })
                {
                    if (color != null)
                    {
                        fillBrush.Color = color.ToSKColor(GetCurrentState().AlphaConstantNonStroking); // todo - check intent, could be stroking
                    }
                    canvas.DrawPath(gp, fillBrush);
                }
            }
        }

        private static bool FontStyleEquals(SKFontStyle fontStyle1, SKFontStyle fontStyle2)
        {
            return fontStyle1.Width == fontStyle2.Width &&
                   fontStyle1.Weight == fontStyle2.Weight &&
                   fontStyle1.Slant == fontStyle2.Slant;
        }

        private SKTypeface GetTypefaceOrFallback(IFont font, string unicode)
        {
            using (var style = font.Details.GetFontStyle())
            {
                if (typefaces.TryGetValue(font.Name, out SKTypeface? drawTypeface) && drawTypeface is not null &&
                    (string.IsNullOrWhiteSpace(unicode) || drawTypeface.ContainsGlyph(unicode[0]))) // Check if can render
                {
                    if (FontStyleEquals(drawTypeface.FontStyle, style))
                    {
                        return drawTypeface;
                    }

                    drawTypeface = SKFontManager.Default.MatchTypeface(drawTypeface, style);
                    if (drawTypeface is not null)
                    {
                        return drawTypeface;
                    }
                }

                string cleanFontName = font.GetCleanFontName();

                drawTypeface = SKTypeface.FromFamilyName(cleanFontName, style);

                //if (!drawTypeface.FamilyName.Equals(cleanFontName, StringComparison.OrdinalIgnoreCase) &&
                //    SystemFontFinder.NameSubstitutes.TryGetValue(cleanFontName, out string[]? subs) && subs != null)
                //{
                //    foreach (var sub in subs)
                //    {
                //        drawTypeface = SKTypeface.FromFamilyName(sub, style);
                //        if (drawTypeface.FamilyName.Equals(sub))
                //        {
                //            break;
                //        }
                //    }
                //}

                // Fallback font
                // https://github.com/mono/SkiaSharp/issues/232
                if (!string.IsNullOrWhiteSpace(unicode) && !drawTypeface.ContainsGlyph(unicode[0]))
                {
                    var fallback = SKFontManager.Default.MatchCharacter(unicode[0]);
                    if (fallback is not null)
                    {
                        drawTypeface = SKFontManager.Default.MatchTypeface(fallback, style);
                    }
                }

                typefaces[font.Name] = drawTypeface;

                return drawTypeface;
            }
        }

        private void ShowNonVectorFontGlyph(IFont font, IColor color, double pointSize, string unicode,
            TransformationMatrix renderingMatrix, TransformationMatrix textMatrix, TransformationMatrix transformationMatrix,
            CharacterBoundingBox characterBoundingBox)
        {
            // Not vector based font
            var transformedGlyphBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, characterBoundingBox.GlyphBounds);

            var transformedPdfBounds = PerformantRectangleTransformer
                .Transform(renderingMatrix, textMatrix, transformationMatrix, new PdfRectangle(0, 0, characterBoundingBox.Width, 0));

            var startBaseLine = transformedPdfBounds.BottomLeft.ToSKPoint(height);
            if (transformedGlyphBounds.Rotation != 0)
            {
                canvas.RotateDegrees((float)-transformedGlyphBounds.Rotation, startBaseLine.X, startBaseLine.Y);
            }

            SKTypeface drawTypeface = GetTypefaceOrFallback(font, unicode);

            var fontPaint = new SKPaint(drawTypeface.ToFont((float)pointSize))
            {
                Style = SKPaintStyle.Fill,
                //BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(), // TODO - check if correct
                Color = (color?.ToSKColor(GetCurrentState().AlphaConstantNonStroking)) ?? SKColors.Black, // todo - check intent, could be stroking
                IsAntialias = antiAliasing
            };

            canvas.DrawText(unicode, startBaseLine, fontPaint);
            canvas.ResetMatrix();

            fontPaint.Dispose();
            drawTypeface.Dispose();
        }

        private static bool CanRender(string unicode)
        {
            if (string.IsNullOrEmpty(unicode?.Trim()))
            {
                return false; // Nothing to render
            }

            // https://character.construction/blanks
            switch (char.GetUnicodeCategory(unicode[0]))
            {
                case System.Globalization.UnicodeCategory.Control:
                case System.Globalization.UnicodeCategory.Format:
                case System.Globalization.UnicodeCategory.ParagraphSeparator:
                case System.Globalization.UnicodeCategory.LineSeparator:
                case System.Globalization.UnicodeCategory.SpaceSeparator:
                    return false;
            }

            return true;
        }
    }
}
