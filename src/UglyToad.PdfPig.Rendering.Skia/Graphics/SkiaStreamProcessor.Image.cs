using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal partial class SkiaStreamProcessor
    {
        public override void RenderXObjectImage(XObjectContentRecord xObjectContentRecord)
        {
            var image = GetImageFromXObject(xObjectContentRecord);
            RenderImage(image);
        }

        public override void RenderInlineImage(InlineImage inlineImage)
        {
            RenderImage(inlineImage);
        }

        private void RenderImage(IPdfImage image)
        {
            var currentState = GetCurrentState();

            // see issue_484Test, Pig production p15
            // need better handling for images where rotation is not 180
            float left = (float)image.Bounds.Left;
            float top = (float)(height - image.Bounds.Top);
            float right = left + (float)image.Bounds.Width;
            float bottom = top + (float)image.Bounds.Height;
            var destRect = new SKRect(left, top, right, bottom);

            try
            {
                try
                {
                    using (var paint = new SKPaint() { IsAntialias = antiAliasing }) // { BlendMode = currentState.BlendMode.ToSKBlendMode() })
                    using (var bitmap = image.GetSKBitmap())
                    {
                        canvas.DrawBitmap(bitmap, destRect, paint);
                    }
                }
                catch (Exception)
                {
                    // Try with raw bytes
                    using (var paint = new SKPaint() { IsAntialias = antiAliasing }) // { BlendMode = currentState.BlendMode.ToSKBlendMode() })
                    using (var bitmap = SKBitmap.Decode(image.RawBytes.ToArray()))
                    {
                        canvas.DrawBitmap(bitmap, destRect, paint);
                    }
                }
            }
            catch (Exception)
            {
                using (var bitmap = new SKBitmap((int)destRect.Width, (int)destRect.Height))
                using (var canvas = new SKCanvas(bitmap))
                using (var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = new SKColor(SKColors.Aquamarine.Red, SKColors.Aquamarine.Green, SKColors.Aquamarine.Blue, 80),
                    IsAntialias = antiAliasing
                })
                {
                    canvas.DrawRect(0, 0, destRect.Width, destRect.Height, paint);
#if DEBUG
                    Directory.CreateDirectory("images_not_rendered");
                    string imagePath = Path.Combine("images_not_rendered", $"{Guid.NewGuid().ToString().ToLower()}.jp2");
                    File.WriteAllBytes(imagePath, image.RawBytes.ToArray());
#endif
                    this.canvas.DrawBitmap(bitmap, destRect);
                }
            }
            finally
            {
                //_canvas.ResetMatrix();
            }
        }
    }
}
