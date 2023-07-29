namespace UglyToad.PdfPig.Graphics
{
    using System;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Logging;

    /// <summary>
    /// Helper class for stream processor.
    /// </summary>
    public static class StreamProcessorHelper
    {
        /// <summary>
        /// Get the initial clipping path using the crop box and the initial transformation matrix.
        /// </summary>
        public static PdfPath GetInitialClipping(CropBox cropBox, TransformationMatrix initialMatrix)
        {
            var transformedCropBox = initialMatrix.Transform(cropBox.Bounds);

            // We re-compute width and height to get possible negative values.
            double width = transformedCropBox.TopRight.X - transformedCropBox.BottomLeft.X;
            double height = transformedCropBox.TopRight.Y - transformedCropBox.BottomLeft.Y;

            // initiate CurrentClippingPath to cropBox
            var clippingSubpath = new PdfSubpath();
            clippingSubpath.Rectangle(transformedCropBox.BottomLeft.X, transformedCropBox.BottomLeft.Y, width, height);
            var clippingPath = new PdfPath() { clippingSubpath };
            clippingPath.SetClipping(FillingRule.EvenOdd);
            return clippingPath;
        }

        /// <summary>
        /// Gets the initial transformation matrix for the stream processor.
        /// </summary>
        [System.Diagnostics.Contracts.Pure]
        public static TransformationMatrix GetInitialMatrix(UserSpaceUnit userSpaceUnit,
            MediaBox mediaBox,
            CropBox cropBox,
            PageRotationDegrees rotation,
            ILog log)
        {
            // Cater for scenario where the cropbox is larger than the mediabox.
            // If there is no intersection (method returns null), fall back to the cropbox.
            var viewBox = mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds;

            if (rotation.Value == 0
                && viewBox.Left == 0
                && viewBox.Bottom == 0
                && userSpaceUnit.PointMultiples == 1)
            {
                return TransformationMatrix.Identity;
            }

            // Move points so that (0,0) is equal to the viewbox bottom left corner.
            var t1 = TransformationMatrix.GetTranslationMatrix(-viewBox.Left, -viewBox.Bottom);

            if (userSpaceUnit.PointMultiples != 1)
            {
                log.Warn("User space unit other than 1 is not implemented");
            }

            // After rotating around the origin, our points will have negative x/y coordinates.
            // Fix this by translating them by a certain dx/dy after rotation based on the viewbox.
            double dx, dy;
            switch (rotation.Value)
            {
                case 0:
                    // No need to rotate / translate after rotation, just return the initial
                    // translation matrix.
                    return t1;
                case 90:
                    // Move rotated points up by our (unrotated) viewbox width
                    dx = 0;
                    dy = viewBox.Width;
                    break;
                case 180:
                    // Move rotated points up/right using the (unrotated) viewbox width/height
                    dx = viewBox.Width;
                    dy = viewBox.Height;
                    break;
                case 270:
                    // Move rotated points right using the (unrotated) viewbox height
                    dx = viewBox.Height;
                    dy = 0;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid value for page rotation: {rotation.Value}.");
            }

            // GetRotationMatrix uses counter clockwise angles, whereas our page rotation
            // is a clockwise angle, so flip the sign.
            var r = TransformationMatrix.GetRotationMatrix(-rotation.Value);

            // Fix up negative coordinates after rotation
            var t2 = TransformationMatrix.GetTranslationMatrix(dx, dy);

            // Now get the final combined matrix T1 > R > T2
            return t1.Multiply(r.Multiply(t2));
        }
    }
}
