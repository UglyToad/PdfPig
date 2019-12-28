namespace UglyToad.PdfPig.Fonts
{
    using System;

    /// <summary>
    /// Specifies various characteristics of a font.
    /// </summary>
    [Flags]
    public enum FontDescriptorFlags
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// All glyphs have the same width.
        /// </summary>
        FixedPitch = 1,
        /// <summary>
        /// Glyphs have serifs.
        /// </summary>
        Serif = 1 << 1,
        /// <summary>
        /// There are glyphs outside the Adobe standard Latin set.
        /// </summary>
        Symbolic = 1 << 2,
        /// <summary>
        /// The glyphs resemble cursive handwriting.
        /// </summary>
        Script = 1 << 3,
        /// <summary>
        /// Font uses a (sub)set of the Adobe standard Latin set.
        /// </summary>
        /// <remarks>Cannot be set at the same time as <see cref="Symbolic"/>.</remarks>
        NonSymbolic = 1 << 5,
        /// <summary>
        /// Font is italic.
        /// </summary>
        Italic = 1 << 6,
        /// <summary>
        /// Font contains only uppercase letters.
        /// </summary>
        AllCap = 1 << 16,
        /// <summary>
        /// Lowercase letters are smaller versions of the uppercase equivalent.
        /// </summary>
        SmallCap = 1 << 17,
        /// <summary>
        /// Forces small bold text to be rendered bold.
        /// </summary>
        ForceBold = 1 << 18
    }
}