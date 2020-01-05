namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using Parser;

    /// <inheritdoc cref="ITrueTypeTable"/>
    /// <summary>
    /// Stores the offset to the glyph locations relative to the start of the <see cref="GlyphDataTable"/>.
    /// Index zero points to the "missing character" which is used for characters not provided by the font.
    /// The number of glpyhs in this table should match the maximum profile table. The glyph offsets contains
    /// an extra entry at the last index which points to the end of the glyph data, this makes it possible to compute
    /// the length of the last glyph entry and supports empty glyphs. 
    /// </summary>
    public class IndexToLocationTable : ITrueTypeTable, IWriteable
    {
        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Loca;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Indicates the format the offsets were stored in in the underlying file, for <see cref="EntryFormat.Short"/>
        /// the values are divided by 2. The values in <see cref="GlyphOffsets"/> are the real offsets, with any format
        /// changes removed.
        /// </summary>
        public EntryFormat Format { get; }

        /// <summary>
        /// The glyph offsets relative to the start of the glyph data table.
        /// </summary>
        public IReadOnlyList<uint> GlyphOffsets { get; }

        /// <summary>
        /// Create a new <see cref="IndexToLocationTable"/>.
        /// </summary>
        public IndexToLocationTable(TrueTypeHeaderTable directoryTable, EntryFormat format, IReadOnlyList<uint> glyphOffsets)
        {
            DirectoryTable = directoryTable;
            Format = format;
            GlyphOffsets = glyphOffsets ?? throw new ArgumentNullException(nameof(glyphOffsets));
        }

        /// <summary>
        /// Load the index to location (loca) table from the TrueType font. Requires the maximum profile (maxp) and header (head) table
        /// to have been parsed.
        /// </summary>
        internal static IndexToLocationTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table, TableRegister.Builder tableRegister)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (tableRegister == null)
            {
                throw new ArgumentNullException(nameof(tableRegister));
            }

            data.Seek(table.Offset);

            var headerTable = tableRegister.HeaderTable;
            var maximumProfileTable = tableRegister.MaximumProfileTable;

            if (headerTable == null)
            {
                throw new InvalidFontFormatException("No header (head) table was defined in this font.");
            }

            if (maximumProfileTable == null)
            {
                throw new InvalidFontFormatException("No maximum profile (maxp) table was defined in this font.");
            }

            var format = (EntryFormat)headerTable.IndexToLocFormat;

            var glyphCount = maximumProfileTable.NumberOfGlyphs + 1;

            uint[] offsets; 

            switch (format)
            {
                case EntryFormat.Short:
                    { 
                        // The local offset divided by 2 is stored.
                        offsets = new uint[glyphCount];
                        for (var i = 0; i < glyphCount; i++)
                        {
                            offsets[i] = (uint)(data.ReadUnsignedShort() * 2);
                        }
                        break;
                    }
                case EntryFormat.Long:
                    {
                        // The actual offset is stored.
                        offsets = data.ReadUnsignedIntArray(glyphCount);
                        break;
                    }
                default:
                    throw new InvalidOperationException($"The format {format} was invalid for the index to location (loca) table.");
            }


            return new IndexToLocationTable(table, format, offsets);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            for (var i = 0; i < GlyphOffsets.Count; i++)
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

        /// <summary>
        /// The format of glyph offset entries stored in the raw TrueType data.
        /// </summary>
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
