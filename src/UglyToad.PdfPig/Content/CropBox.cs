namespace UglyToad.PdfPig.Content
{
    using Geometry;

    /// <summary>
    /// Defines the visible region of a page, contents expanding beyond the crop box should be clipped.
    /// </summary>
    public class CropBox
    {
        /// <summary>
        /// Defines the clipping of the content when the page is displayed or printed. The page's contents are to be clipped (cropped) to this rectangle
        /// and then imposed on the output medium.
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// Create a new <see cref="CropBox"/>.
        /// </summary>
        public CropBox(PdfRectangle bounds)
        {
            Bounds = bounds;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Bounds.ToString();
        }
    }
}