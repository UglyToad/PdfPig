namespace UglyToad.PdfPig.Fonts.TrueType.Names
{
    /// <summary>
    /// The meaning of the platform specific encoding when the <see cref="TrueTypePlatformIdentifier"/> is <see cref="TrueTypePlatformIdentifier.Unicode"/>.
    /// </summary>
    internal enum TrueTypeUnicodeEncodingIndentifier
    {
        /// <summary>
        /// Default semantics.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Version 1.1 semantics.
        /// </summary>
        Version1Point1 = 1,
        /// <summary>
        /// ISO 10646 1993 semantics (deprecated).
        /// </summary>
        Iso10646 = 2,
        /// <summary>
        /// Unicode 2.0 and above semantics for BMP characters only.
        /// </summary>
        Unicode2BmpOnly = 3,
        /// <summary>
        /// Uncidoe 2.0 and above semantics including non-BMP characters.
        /// </summary>
        Unicode2NonBmpAllowed = 4,
        /// <summary>
        /// Unicode Variation Sequences.
        /// </summary>
        UnicodeVariationSequences = 5,
        /// <summary>
        /// Full Unicode coverage.
        /// </summary>
        FullUnicode = 6
    }
}