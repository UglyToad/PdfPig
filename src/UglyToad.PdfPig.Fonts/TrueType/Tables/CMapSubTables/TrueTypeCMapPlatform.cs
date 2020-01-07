namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    /// <summary>
    /// The platform identifier for a CMap table.
    /// </summary>
    public enum TrueTypeCMapPlatform : ushort
    {
        /// <summary>
        /// Unicode.
        /// </summary>
        Unicode = 0,
        /// <summary>
        /// Apple Macintosh.
        /// </summary>
        Macintosh = 1,
        /// <summary>
        /// Unused.
        /// </summary>
        Reserved2 = 2,
        /// <summary>
        /// Microsoft Windows.
        /// </summary>
        Windows = 3
    }
}