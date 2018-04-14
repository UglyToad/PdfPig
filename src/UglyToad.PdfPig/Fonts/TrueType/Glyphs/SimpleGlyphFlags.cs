namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using System;

    [Flags]
    internal enum SimpleGlyphFlags : byte
    {
        /// <summary>
        /// The point is on the curve.
        /// </summary>
        OnCurve = 1,
        /// <summary>
        /// The x-coordinate is 1 byte long instead of 2.
        /// </summary>
        XShortVector = 1 << 1,
        /// <summary>
        /// The y-coordinate is 1 byte long instead of 2.
        /// </summary>
        YShortVector = 1 << 2,
        /// <summary>
        /// The next byte specifies the number of times to repeat this set of flags.
        /// </summary>
        Repeat = 1 << 3,
        /// <summary>
        /// If <see cref="XShortVector"/> is set this means the sign of the x-coordinate is positive.
        /// If <see cref="XShortVector"/> is not set then the current x-coordinate is the same as the previous.
        /// </summary>
        XSignOrSame = 1 << 4,
        /// <summary>
        /// If <see cref="YShortVector"/> is set this means the sign of the y-coordinate is positive.
        /// If <see cref="YShortVector"/> is not set then the current y-coordinate is the same as the previous.
        /// </summary>
        YSignOrSame = 1 << 5
    }
}