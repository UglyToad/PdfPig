using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Rendering.Skia.Graphics
{
    internal partial class SkiaStreamProcessor
    {
        /// <summary>
        /// Very hackish
        /// </summary>
        private static bool IsAnnotationBelowText(Annotation annotation)
        {
            switch (annotation.Type)
            {
                case AnnotationType.Highlight:
                    return true;

                default:
                    return false;
            }
        }

        private void DrawAnnotations(bool isBelowText)
        {
            // https://github.com/apache/pdfbox/blob/trunk/pdfbox/src/main/java/org/apache/pdfbox/rendering/PageDrawer.java
            // https://github.com/apache/pdfbox/blob/c4b212ecf42a1c0a55529873b132ea338a8ba901/pdfbox/src/main/java/org/apache/pdfbox/contentstream/PDFStreamEngine.java#L312
            foreach (var annotation in page.ExperimentalAccess.GetAnnotations().Where(a => IsAnnotationBelowText(a) == isBelowText))
            {
                // Check if visible

                // Get appearance
                var appearance = base.GetNormalAppearanceAsStream(annotation);

                PdfRectangle? bbox = null;
                PdfRectangle? rect = annotation.Rectangle;

                if (appearance is not null)
                {
                    if (appearance.StreamDictionary.TryGet<ArrayToken>(NameToken.Bbox, out var bboxToken))
                    {
                        var points = bboxToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();
                        bbox = new PdfRectangle(points[0], points[1], points[2], points[3]);
                    }

                    // zero-sized rectangles are not valid
                    if (rect.HasValue && rect.Value.Width > 0 && rect.Value.Height > 0 &&
                        bbox.HasValue && bbox.Value.Width > 0 && bbox.Value.Height > 0)
                    {
                        var matrix = TransformationMatrix.Identity;
                        if (appearance.StreamDictionary.TryGet<ArrayToken>(NameToken.Matrix, out var matrixToken))
                        {
                            matrix = TransformationMatrix.FromArray(matrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray());
                        }

                        PushState();

                        // transformed appearance box  fixme: may be an arbitrary shape
                        PdfRectangle transformedBox = matrix.Transform(bbox.Value).Normalise();

                        // Matrix a = Matrix.getTranslateInstance(rect.getLowerLeftX(), rect.getLowerLeftY());
                        TransformationMatrix a = TransformationMatrix.GetTranslationMatrix(rect.Value.TopLeft.X, rect.Value.TopLeft.Y);
                        a = Scale(a, (float)(rect.Value.Width / transformedBox.Width), (float)(rect.Value.Height / transformedBox.Height));
                        a = a.Translate(-transformedBox.TopLeft.X, -transformedBox.TopLeft.Y);

                        // Matrix shall be concatenated with A to form a matrix AA that maps from the appearance's
                        // coordinate system to the annotation's rectangle in default user space
                        //
                        // HOWEVER only the opposite order works for rotated pages with 
                        // filled fields / annotations that have a matrix in the appearance stream, see PDFBOX-3083
                        //Matrix aa = Matrix.concatenate(a, matrix);
                        //TransformationMatrix aa = a.Multiply(matrix);

                        GetCurrentState().CurrentTransformationMatrix = a;

                        try
                        {
                            base.ProcessFormXObject(appearance);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"DrawAnnotations: {ex}");
                        }
                        finally
                        {
                            PopState();
                        }
                    }
                }
                else
                {
                    DebugDrawRect(annotation.Rectangle);
                }
            }
        }

        private static TransformationMatrix Scale(TransformationMatrix matrix, float sx, float sy)
        {
            var x0 = matrix[0, 0] * sx;
            var x1 = matrix[0, 1] * sx;
            var x2 = matrix[0, 2] * sx;
            var y0 = matrix[1, 0] * sy;
            var y1 = matrix[1, 1] * sy;
            var y2 = matrix[1, 2] * sy;
            return new TransformationMatrix(x0, x1, x2, y0, y1, y2, matrix[2, 0], matrix[2, 1], matrix[2, 2]);
        }
    }
}
