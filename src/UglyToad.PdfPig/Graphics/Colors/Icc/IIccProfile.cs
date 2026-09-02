namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;

    /// <summary>
    /// A parsed ICC profile bound to a fixed input-component count, but independent of rendering intent.
    /// Implementations cache per-intent <see cref="IIccTransform"/> handles internally so repeated calls
    /// with the same intent return the same instance. Must be safe for concurrent reads (a single instance
    /// is shared across every paint and image operation that references the corresponding
    /// <see cref="ICCBasedColorSpaceDetails"/>).
    /// </summary>
    public interface IIccProfile
    {
        /// <summary>
        /// The profile's own input component count, derived from its data colour space.
        /// </summary>
        int NumberOfComponents { get; }

        /// <summary>
        /// The valid range of each input component in the profile's own data colour space, as
        /// 2 x <see cref="NumberOfComponents"/> values <c>[min0 max0 min1 max1 ...]</c>. This is the
        /// counterpart of <c>ICC_ColorSpace.getMinValue</c> / <c>getMaxValue</c>, which is where PDFBox
        /// takes the same information from.
        /// <para>
        /// Read once when the colour space is built, so implementations need not cache it.
        /// </para>
        /// </summary>
        IReadOnlyList<double> ComponentRanges { get; }

        /// <summary>
        /// Resolve a transform for the given rendering intent.
        /// Returning <c>false</c> means the backend cannot honour the requested intent; the caller may retry with
        /// <see cref="RenderingIntent.RelativeColorimetric"/> (the PDF default) or fall back to the alternate color space.
        /// </summary>
        bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform);
    }
}
