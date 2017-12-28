namespace UglyToad.Pdf.Content
{
    using System;
    using Geometry;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Defines the visible region, contents expanding beyond the crop box should be clipped.
    /// </summary>
    public class CropBox
    {
        [NotNull]
        public PdfRectangle Bounds { get; }

        public CropBox(PdfRectangle bounds)
        {
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        }
    }
}