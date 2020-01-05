namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using System;

    /// <summary>
    /// Specifies the meaning of each coordinate in the simple glyph definition.
    /// </summary>
    [Flags]
    public enum SimpleGlyphFlags : byte
    {
        /// <summary>
        /// The point is on the curve.
        /// </summary>
        OnCurve = 1,
        /// <summary>
        /// The x-coordinate is 1 byte long instead of 2.
        /// </summary>
        XSingleByte = 1 << 1,
        /// <summary>
        /// The y-coordinate is 1 byte long instead of 2.
        /// </summary>
        YSingleByte = 1 << 2,
        /// <summary>
        /// The next byte specifies the number of times to repeat this set of flags.
        /// </summary>
        Repeat = 1 << 3,
        /// <summary>
        /// If <see cref="XSingleByte"/> is set this means the sign of the x-coordinate is positive.
        /// If <see cref="XSingleByte"/> is not set then the current x-coordinate is the same as the previous.
        /// </summary>
        ThisXIsTheSame = 1 << 4,
        /// <summary>
        /// If <see cref="YSingleByte"/> is set this means the sign of the y-coordinate is positive.
        /// If <see cref="YSingleByte"/> is not set then the current y-coordinate is the same as the previous.
        /// </summary>
        ThisYIsTheSame = 1 << 5
    }
}