namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;

    /// <summary>
    /// A parsed ICC profile bound to a rendering intent. Implementations must
    /// be safe for concurrent reads; a single instance is shared across paint
    /// and image conversions for a given <see cref="ICCBasedColorSpaceDetails"/>.
    /// </summary>
    public interface IIccTransform
    {
        /// <summary>Number of device-space color components consumed.</summary>
        int NumberOfComponents { get; }

        /// <summary>
        /// Convert one device-space color (each value in [0..1]) to sRGB
        /// in [0..1].
        /// </summary>
        (double r, double g, double b) ToRgb(ReadOnlySpan<double> values);

        /// <summary>
        /// Convert a packed device-space sample buffer (8-bit per component,
        /// <see cref="NumberOfComponents"/> components per pixel) to
        /// interleaved sRGB bytes (3 bytes per pixel).
        /// </summary>
        /// <param name="src">Source. Length must be <c>pixelCount * NumberOfComponents</c>.</param>
        /// <param name="dstRgb">Destination. Length must be at least <c>pixelCount * 3</c>.</param>
        void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb);
    }
}