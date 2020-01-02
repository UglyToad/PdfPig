namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.IO;
    using IO;
    using Parser;
    using Util;

    /// <summary>
    /// Stores the offset to the glyph locations relative to the start of the glyph data table.
    /// Index zero points to the "missing character" which is used for characters not provided by the font.
    /// The number of glpyhs in this table should match the maximum profile table.
    /// </summary>
    internal class IndexToLocationTable : ITable, IWriteable
    {
        public string Tag => TrueTypeHeaderTable.Loca;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public EntryFormat Format { get; }

        /// <summary>
        /// The glyph offsets relative to the start of the glyph data table.
        /// </summary>
        public long[] GlyphOffsets { get; }

        public IndexToLocationTable(TrueTypeHeaderTable directoryTable, EntryFormat format, long[] glyphOffsets)
        {
            DirectoryTable = directoryTable;
            Format = format;
            GlyphOffsets = glyphOffsets;
        }

        public static IndexToLocationTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister.Builder tableRegister)
        {
            data.Seek(table.Offset);

            var headerTable = tableRegister.HeaderTable;
            var maximumProfileTable = tableRegister.MaximumProfileTable;

            var format = (EntryFormat)headerTable.IndexToLocFormat;

            var glyphCount = maximumProfileTable.NumberOfGlyphs + 1;

            var offsets = new long[glyphCount];

            switch (format)
            {
                case EntryFormat.Short:
                    { // The local offset divided by 2 is stored.
                        for (int i = 0; i < glyphCount; i++)
                        {
                            offsets[i] = data.ReadUnsignedShort() * 2;
                        }
                        break;
                    }
                case EntryFormat.Long:
                    {
                        // The actual offset is stored.
                        data.ReadUnsignedIntArray(offsets, glyphCount);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"The format {format} was invalid for the index to location (loca) table.");
            }


            return new IndexToLocationTable(table, format, offsets);
        }

        public void Write(Stream stream)
        {
            for (var i = 0; i < GlyphOffsets.Length; i++)
            {
                var offset = GlyphOffsets[i];
                switch (Format)
                {
                    case EntryFormat.Short:
                        stream.WriteUShort((ushort)offset/2);
                        break;
                    case EntryFormat.Long:
                        stream.WriteUInt(offset);
                        break;
                    default:
                        throw new InvalidOperationException($"The format {Format} was invalid for the index to location (loca) table.");
                }
            }
        }

        public enum EntryFormat : short
        {
            /// <summary>
            /// The actual local offset divided by 2 is stored.
            /// </summary>
            Short = 0,
            /// <summary>
            /// The actual local offset is stored.
            /// </summary>
            Long = 1
        }
    }
}
