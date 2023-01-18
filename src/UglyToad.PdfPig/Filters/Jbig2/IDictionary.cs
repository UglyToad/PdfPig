namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface for all JBIG2 dictionaries segments.
    /// </summary>
    internal interface IDictionary : ISegmentData
    {
        /// <summary>
        /// Decodes a dictionary segment and returns the result.
        /// </summary>
        /// <returns>A list of <see cref="Bitmap"/>s as a result of the decoding process of dictionary segments.</returns>
        /// <exception cref="InvalidHeaderValueException">if the segment header value is invalid.</exception>
        /// <exception cref="IntegerMaxValueException">if the maximum value limit of an integer is exceeded.</exception>
        /// <exception cref="System.IO.IOException">if an underlying IO operation fails.</exception>
        List<Bitmap> GetDictionary();
    }
}
