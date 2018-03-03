namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using Parser;

    /// <summary>
    /// Stores the offset to the glyph locations relative to the start of the glyph data table.
    /// Index zero points to the "missing character" which is used for characters not provided by the font.
    /// The number of glpyhs in this table should match the maximum profile table.
    /// </summary>
    internal class IndexToLocationTable : ITable
    {
        public string Tag => TrueTypeHeaderTable.Loca;

        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// The glyph offsets relative to the start of the glyph data table.
        /// </summary>
        public long[] GlyphOffsets { get; }

        public IndexToLocationTable(TrueTypeHeaderTable directoryTable, long[] glyphOffsets)
        {
            DirectoryTable = directoryTable;
            GlyphOffsets = glyphOffsets;
        }

        public static IndexToLocationTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister tableRegister)
        {
            const short shortFormat = 0;
            const short longFormat = 1;

            data.Seek(table.Offset);

            var headerTable = tableRegister.HeaderTable;
            var maximumProfileTable = tableRegister.MaximumProfileTable;

            var format = headerTable.IndexToLocFormat;

            var glyphCount = maximumProfileTable.NumberOfGlyphs + 1;

            var offsets = new long[glyphCount];

            switch (format)
            {
                case shortFormat:
                    { // The local offset divided by 2 is stored.
                        for (int i = 0; i < glyphCount; i++)
                        {
                            offsets[i] = data.ReadUnsignedShort() * 2;
                        }
                        break;
                    }
                case longFormat:
                    {
                        // The actual offset is stored.
                        data.ReadUnsignedIntArray(offsets, glyphCount);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"The format {format} was invalid for the index to location (loca) table.");
            }


            return new IndexToLocationTable(table, offsets);
        }
    }
}
