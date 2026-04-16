namespace UglyToad.PdfPig
{
    using System;

    /// <summary>
    /// Flags controlling which content types are extracted when processing PDF pages.
    /// Only the requested capabilities will be processed, the rest will be skipped.
    /// </summary>
    [Flags]
    public enum PdfCapabilities
    {
        /// <summary>
        /// Extract text content (letters, words).
        /// </summary>
        Text = 1 << 0,

        /// <summary>
        /// Extract images (both XObject and inline).
        /// </summary>
        Images = 1 << 1,

        /// <summary>
        /// Extract paths (lines, curves, rectangles).
        /// </summary>
        Paths = 1 << 2,

        /// <summary>
        /// Extract all supported content types.
        /// </summary>
        All = Text | Images | Paths
    }
}
