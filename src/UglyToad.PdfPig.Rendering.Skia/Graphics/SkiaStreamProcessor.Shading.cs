using SkiaSharp;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics.Colors;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal partial class SkiaStreamProcessor
    {
        protected override void RenderShading(Shading shading)
        {
            float maxX = canvas.DeviceClipBounds.Right;
            float maxY = canvas.DeviceClipBounds.Top;
            float minX = canvas.DeviceClipBounds.Left;
            float minY = canvas.DeviceClipBounds.Bottom;

            switch (shading.ShadingType)
            {
                case ShadingType.Axial:
                    RenderAxialShading(shading as AxialShading, CurrentTransformationMatrix, minX, minY, maxX, maxY);
                    break;

                case ShadingType.Radial:
                    RenderRadialShading(shading as RadialShading, CurrentTransformationMatrix, minX, minY, maxX, maxY);
                    break;

                case ShadingType.FunctionBased:
                case ShadingType.FreeFormGouraud:
                case ShadingType.LatticeFormGouraud:
                case ShadingType.CoonsPatch:
                case ShadingType.TensorProductPatch:
                default:
                    RenderUnsupportedShading(shading, CurrentTransformationMatrix);
                    break;
            }
        }

        private void RenderUnsupportedShading(Shading shading, TransformationMatrix transformationMatrix)
        {
            var (x0, y0) = transformationMatrix.Transform(0, 0);
            var (x1, y1) = transformationMatrix.Transform(0, 1);

            float xs0 = (float)x0;
            float ys0 = (float)(height - y0);
            float xs1 = (float)x1;
            float ys1 = (float)(height - y1);
            using (var paint = new SKPaint() { IsAntialias = shading.AntiAlias })
            {
                //paint.BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(); // TODO - check if correct

                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(xs0, ys0),
                    new SKPoint(xs1, ys1),
                    new[]
                    {
                        SKColors.Red,
                        SKColors.Green
                    },
                    SKShaderTileMode.Clamp);

                // check if bbox not null

                canvas.DrawPaint(paint);
            }
        }

        private void RenderRadialShading(RadialShading shading, TransformationMatrix transformationMatrix, float minX, float minY, float maxX, float maxY,
            bool isStroke = false, SKPath? path = null)
        {
            // Not correct
            var coords = shading.Coords.Select(c => (float)c).ToArray();
            var domain = shading.Domain.Select(c => (float)c).ToArray();

            float r0 = coords[2];
            float r1 = coords[5];

            // If one radius is 0, the corresponding circle shall be treated as a point;
            // if both are 0, nothing shall be painted.
            if (r0 == 0 && r1 == 0)
            {
                return;
            }

            (double x0, double y0) = transformationMatrix.Transform(coords[0], coords[1]);
            (double x1, double y1) = transformationMatrix.Transform(coords[3], coords[4]);

            float xs0 = (float)x0;
            float ys0 = (float)(height - y0);
            float xs1 = (float)x1;
            float ys1 = (float)(height - y1);
            float r0s = (float)r0;
            float r1s = (float)r1;

            var colors = new List<SKColor>();
            float t0 = domain[0];
            float t1 = domain[1];

            // worst case for the number of steps is opposite diagonal corners, so use that
            double dist = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2));
            int factor = (int)Math.Ceiling(dist); // too much?
            for (int t = 0; t <= factor; t++)
            {
                double tx = t0 + (t / (double)factor) * t1;
                double[] v = shading.Eval(tx);
                IColor c = shading.ColorSpace.GetColor(v);
                colors.Add(c.ToSKColor(GetCurrentState().AlphaConstantNonStroking)); // TODO - is it non stroking??
            }

            if (shading.BBox.HasValue)
            {

            }

            if (shading.Background != null)
            {

            }

            if (r0s == 0)
            {
                using (var paint = new SKPaint() { IsAntialias = shading.AntiAlias })
                {
                    //paint.BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(); // TODO - check if correct

                    paint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint((float)xs1, (float)ys1),
                        r1s * (float)dist,
                        colors.ToArray(),
                        SKShaderTileMode.Clamp);

                    // check if bbox not null

                    canvas.DrawPaint(paint);
                }
            }
            else if (r1s == 0)
            {
                using (var paint = new SKPaint() { IsAntialias = shading.AntiAlias })
                {
                    //paint.BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(); // TODO - check if correct

                    paint.Shader = SKShader.CreateRadialGradient(
                        new SKPoint((float)xs0, (float)ys0),
                        r0s * (float)dist,
                        colors.ToArray(),
                        SKShaderTileMode.Clamp);

                    // check if bbox not null

                    canvas.DrawPaint(paint);
                }
            }
            else
            {
                using (var paint = new SKPaint() { IsAntialias = shading.AntiAlias })
                {
                    //paint.BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(); // TODO - check if correct

                    paint.Shader = SKShader.CreateTwoPointConicalGradient(
                        new SKPoint((float)xs0, (float)ys0),
                        r0s * (float)dist,
                        new SKPoint((float)xs1, (float)ys1),
                        r1s * (float)dist,
                        colors.ToArray(),
                        SKShaderTileMode.Clamp);

                    // check if bbox not null

                    if (isStroke)
                    {
                        // TODO - To Check
                        paint.Style = SKPaintStyle.Stroke;
                        paint.StrokeWidth = Math.Max(0.5f, GetScaledLineWidth()); // A guess
                        paint.StrokeJoin = GetCurrentState().JoinStyle.ToSKStrokeJoin();
                        paint.StrokeCap = GetCurrentState().CapStyle.ToSKStrokeCap();
                        paint.PathEffect = GetCurrentState().LineDashPattern.ToSKPathEffect();
                    }

                    if (path is null)
                    {
                        canvas.DrawPaint(paint);
                    }
                    else
                    {
                        canvas.DrawPath(CurrentPath, paint);
                    }
                }
            }
        }

        private void RenderAxialShading(AxialShading shading, TransformationMatrix transformationMatrix, float minX, float minY, float maxX, float maxY,
            bool isStroke = false, SKPath? path = null)
        {
            var coords = shading.Coords.Select(c => (float)c).ToArray();
            var domain = shading.Domain.Select(c => (float)c).ToArray();

            (double x0, double y0) = transformationMatrix.Transform(coords[0], coords[1]);
            (double x1, double y1) = transformationMatrix.Transform(coords[2], coords[3]);

            float xs0 = (float)x0;
            float ys0 = (float)(height - y0);
            float xs1 = (float)x1;
            float ys1 = (float)(height - y1);

            var colors = new List<SKColor>();
            float t0 = domain[0];
            float t1 = domain[1];

            if (shading.BBox.HasValue)
            {

            }

            if (shading.Background != null)
            {

            }

            // worst case for the number of steps is opposite diagonal corners, so use that
            double dist = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2));
            int factor = Math.Max(10, (int)Math.Ceiling(dist)); // too much? - Min of 10

            for (int t = 0; t <= factor; t++)
            {
                double tx = t0 + (t / (double)factor) * t1;
                double[] v = shading.Eval(tx);
                IColor c = shading.ColorSpace.GetColor(v);
                colors.Add(c.ToSKColor(GetCurrentState().AlphaConstantNonStroking)); // TODO - is it non stroking??
            }

            using (var paint = new SKPaint() { IsAntialias = shading.AntiAlias })
            {
                //paint.BlendMode = GetCurrentState().BlendMode.ToSKBlendMode(); // TODO - check if correct

                paint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(xs0, ys0),
                    new SKPoint(xs1, ys1),
                    colors.ToArray(),
                    SKShaderTileMode.Clamp);

                // check if bbox not null

                if (isStroke)
                {
                    // TODO - To Check
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = Math.Max(0.5f, GetScaledLineWidth()); // A guess
                    paint.StrokeJoin = GetCurrentState().JoinStyle.ToSKStrokeJoin();
                    paint.StrokeCap = GetCurrentState().CapStyle.ToSKStrokeCap();
                    paint.PathEffect = GetCurrentState().LineDashPattern.ToSKPathEffect();
                }

                if (path is null)
                {
                    canvas.DrawPaint(paint);
                }
                else
                {
                    canvas.DrawPath(CurrentPath, paint);
                }
            }
        }

        private void RenderShadingPattern(ShadingPatternColor pattern, bool isStroke)
        {
            if (pattern.ExtGState != null)
            {
                // TODO
            }

            TransformationMatrix transformationMatrix = pattern.Matrix.Multiply(CurrentTransformationMatrix);

            float maxX = CurrentPath!.Bounds.Right;
            float maxY = CurrentPath.Bounds.Top;
            float minX = CurrentPath.Bounds.Left;
            float minY = CurrentPath.Bounds.Bottom;

            switch (pattern.Shading.ShadingType)
            {
                case ShadingType.Axial:
                    RenderAxialShading(pattern.Shading as AxialShading, transformationMatrix, minX, minY, maxX, maxY, isStroke, CurrentPath);
                    break;

                case ShadingType.Radial:
                    RenderRadialShading(pattern.Shading as RadialShading, transformationMatrix, minX, minY, maxX, maxY, isStroke, CurrentPath);
                    break;

                case ShadingType.FunctionBased:
                case ShadingType.FreeFormGouraud:
                case ShadingType.LatticeFormGouraud:
                case ShadingType.CoonsPatch:
                case ShadingType.TensorProductPatch:
                default:
                    RenderUnsupportedShading(pattern.Shading, CurrentTransformationMatrix);
                    break;
            }
        }
    }
}
