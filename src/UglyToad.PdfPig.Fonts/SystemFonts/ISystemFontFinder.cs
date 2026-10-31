namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using TrueType;

    /// <summary>
    /// Used to find named fonts from the host operating/file system.
    /// </summary>
    public interface ISystemFontFinder
    {
        /// <summary>
        /// Get the TrueType font with the specified name, or <see langword="null"/> if there is none.
        /// </summary>
        TrueTypeFont? GetTrueTypeFont(string name);
    }
}