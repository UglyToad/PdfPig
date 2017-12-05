namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    /// <summary>
    /// Stores the offset to the glyph locations relative to the start of the glyph data table.
    /// Index zero points to the "missing character" which is used for characters not provided by the font.
    /// The number of glpyhs in this table should match the maximum profile table.
    /// </summary>
    internal class IndexToLocationTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Loca;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public long[] GlyphOffsets { get; }

        public IndexToLocationTable(TrueTypeHeaderTable directoryTable, long[] glyphOffsets)
        {
            DirectoryTable = directoryTable;
            GlyphOffsets = glyphOffsets;
        }

        public static IndexToLocationTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table)
        {
            data.Seek(table.Offset);

            return null;
        }
    }
}
