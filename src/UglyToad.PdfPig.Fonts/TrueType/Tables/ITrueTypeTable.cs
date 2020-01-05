namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    /// <summary>
    /// A table in a TrueType font.
    /// </summary>
    public interface ITrueTypeTable
    {
        /// <summary>
        /// The tag, a 4 letter/byte code, used to identify this table in a TrueType font.
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// The directory entry from the font's offset subtable which indicates the length, offset, type and checksum of a table.
        /// </summary>
        TrueTypeHeaderTable DirectoryTable { get; }
    }
}
