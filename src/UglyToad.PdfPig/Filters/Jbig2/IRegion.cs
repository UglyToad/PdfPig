namespace UglyToad.PdfPig.Filters.Jbig2
{
    /// <summary>
    /// Interface for all JBIG2 region segments.
    /// </summary>
    internal interface IRegion : ISegmentData
    {
        /// <summary>
        /// Returns <see cref="RegionSegmentInformation"/> about this region.
        /// </summary>
        RegionSegmentInformation RegionInfo { get; }

        /// <summary>
        /// Decodes and returns a regions content.
        /// </summary>
        /// <returns>The decoded region as <see cref="Bitmap"/>.</returns>
        /// <exception cref="InvalidHeaderValueException">if the segment header value is invalid.</exception>
        /// <exception cref="IntegerMaxValueException">if the maximum value limit of an integer is exceeded.</exception>
        /// <exception cref="System.IO.IOException">if an underlying IO operation fails.</exception>
        Bitmap GetRegionBitmap();
    }
}
