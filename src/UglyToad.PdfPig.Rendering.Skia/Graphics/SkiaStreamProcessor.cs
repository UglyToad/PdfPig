using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Parser;
using UglyToad.PdfPig.Tokenization.Scanner;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal sealed partial class SkiaStreamProcessor : BaseRenderStreamProcessor<SKPicture>
    {
        private readonly int height;
        private readonly int width;

        private SKCanvas canvas;
        private readonly Page page;

        private readonly bool antiAliasing = true;

        private readonly Dictionary<string, SKTypeface> typefaces = new Dictionary<string, SKTypeface>();

        public SkiaStreamProcessor(
            int pageNumber,
            IResourceStore resourceStore,
            UserSpaceUnit userSpaceUnit,
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            ILookupFilterProvider filterProvider,
            IParsingOptions parsingOptions)
            : base(pageNumber, resourceStore, userSpaceUnit, mediaBox, cropBox, rotation, pdfScanner, pageContentParser, filterProvider, parsingOptions)
        {
            // Special case where cropbox is outside mediabox: use cropbox instead of intersection
            var viewBox = mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds;

            width = (int)(rotation.SwapsAxis ? viewBox.Height : viewBox.Width);
            height = (int)(rotation.SwapsAxis ? viewBox.Width : viewBox.Height);
        }

        public override SKPicture Process(int pageNumberCurrent, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            // https://github.com/apache/pdfbox/blob/94b3d15fd24b9840abccece261173593625ff85c/pdfbox/src/main/java/org/apache/pdfbox/rendering/PDFRenderer.java#L274

            CloneAllStates();

            using (var recorder = new SKPictureRecorder())
            using (canvas = recorder.BeginRecording(SKRect.Create(width, height)))
            {
                canvas.Clear(SKColors.White);

                // TODO Annotation to render (maybe in a different layer)

                //DrawAnnotations(true);

                ProcessOperations(operations);

                //DrawAnnotations(false);

                canvas.Flush();

                return recorder.EndRecording();
            }
        }

        private void DebugDrawRect(SKRect destRect)
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                // https://learn.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/effects
                SKPathEffect diagLinesPath = SKPathEffect.Create2DLine(4,
                    SKMatrix.Concat(SKMatrix.CreateScale(6, 6), SKMatrix.CreateRotationDegrees(45)));

                var fillBrush = new SKPaint()
                {
                    Style = SKPaintStyle.Fill,
                    Color = new SKColor(SKColors.Aqua.Red, SKColors.Aqua.Green, SKColors.Aqua.Blue),
                    PathEffect = diagLinesPath,
                    IsAntialias = antiAliasing
                };
                canvas.DrawRect(destRect, fillBrush);

                fillBrush = new SKPaint()
                {
                    Style = SKPaintStyle.Stroke,
                    Color = new SKColor(SKColors.Red.Red, SKColors.Red.Green, SKColors.Red.Blue),
                    StrokeWidth = 5,
                    IsAntialias = antiAliasing
                };
                canvas.DrawRect(destRect, fillBrush);

                diagLinesPath.Dispose();
                fillBrush.Dispose();
            }
        }

        private void DebugDrawRect(PdfRectangle rect)
        {
            var upperLeft = rect.TopLeft.ToSKPoint(height);
            var destRect = new SKRect(upperLeft.X, upperLeft.Y,
                             upperLeft.X + (float)(rect.Width),
                             upperLeft.Y + (float)(rect.Height)).Standardized;
            DebugDrawRect(destRect);
        }

        public override void PopState()
        {
            base.PopState();
            canvas.Restore();
        }

        public override void PushState()
        {
            base.PushState();
            canvas.Save();
        }

        public override void ModifyClippingIntersect(FillingRule clippingRule)
        {
            if (CurrentPath == null)
            {
                return;
            }

            CurrentPath.FillType = clippingRule.ToSKPathFillType();
            canvas.ClipPath(CurrentPath, SKClipOperation.Intersect);
        }
    }
}
