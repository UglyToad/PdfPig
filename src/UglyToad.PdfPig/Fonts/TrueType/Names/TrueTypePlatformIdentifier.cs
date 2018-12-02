namespace UglyToad.PdfPig.Fonts.TrueType.Names
{
    /// <summary>
    /// The platform identifier for a TrueType/OpenType font allows for platform specific implementations.
    /// </summary>
    internal enum TrueTypePlatformIdentifier
    {
        /// <summary>
        /// Unicode
        /// </summary>
        Unicode = 0,
        /// <summary>
        /// Macintosh.
        /// </summary>
        Macintosh = 1,
        /// <summary>
        /// The platform identifier 2 was originally to use with ISO 10646, but is now deprecated, as it and Unicode have identical character code assignments.
        /// </summary>
        Iso = 2,
        /// <summary>
        /// Microsoft Windows.
        /// </summary>
        Windows = 3
    }
}
