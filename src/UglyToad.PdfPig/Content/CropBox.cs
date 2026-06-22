namespace UglyToad.PdfPig.Content
{
    using Core;

    /// <summary>
    /// Defines the visible region of a page, contents expanding beyond the crop box should be clipped.
    /// <para>
    /// If the bounds of the crop box extend outside of the bounds of the media box, a processor
    /// shall treat the crop box as its intersection with the media box. When the two do not intersect
    /// at all (malformed input), fall back to the declared crop box.
    /// </para>
    /// </summary>
    public sealed class CropBox
    {
        /// <summary>
        /// A rectangle, expressed in unrotated default user space units, that defines the clipping of the content when the page is displayed or printed.
        /// The page's contents are to be clipped (cropped) to this rectangle and then imposed on the output medium.
        /// <para>
        /// If the bounds of the crop box extend outside of the bounds of the media box, a processor
        /// shall treat the crop box as its intersection with the media box. When the two do not intersect
        /// at all (malformed input), fall back to the declared crop box.
        /// </para>
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// Create a new <see cref="CropBox"/>.
        /// </summary>
        public CropBox(PdfRectangle bounds)
        {
            Bounds = bounds;
        }

        /// <summary>
        /// Gets the visible page bounds, in display space, taking the page rotation into account.
        /// <para>
        /// The crop box is expressed in unrotated default user space. The returned rectangle has its
        /// origin at (0, 0) — matching the coordinate system content is rendered into — and, for a 90
        /// or 270 degree rotation, its width and height swapped (see
        /// <see cref="PageRotationDegrees.SwapsAxis"/>). Its <see cref="PdfRectangle.Width"/> and
        /// <see cref="PdfRectangle.Height"/> are therefore the visible page dimensions in points.
        /// </para>
        /// </summary>
        /// <param name="rotation">The page rotation.</param>
        public PdfRectangle GetVisibleBounds(PageRotationDegrees rotation)
        {
            return rotation.SwapsAxis
                ? new PdfRectangle(0, 0, Bounds.Height, Bounds.Width)
                : new PdfRectangle(0, 0, Bounds.Width, Bounds.Height);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Bounds.ToString();
        }
    }
}