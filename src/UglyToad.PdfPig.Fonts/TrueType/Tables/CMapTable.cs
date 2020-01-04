namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using CMapSubTables;
    using Core;

    /// <inheritdoc cref="ITrueTypeTable"/>.
    /// <summary>
    /// The cmap table maps character codes to glyph indices.
    /// The choice of encoding for a particular font is dependent on the conventions used by the intended platform.
    /// The cmap table can contain multiple encoding tables for use on different platforms, one for each supported encoding scheme.
    /// </summary>
    public class CMapTable : ITrueTypeTable, IWriteable
    {
        /// <inheritdoc />
        public string Tag => TrueTypeHeaderTable.Cmap;

        /// <inheritdoc />
        public TrueTypeHeaderTable DirectoryTable { get; }

        /// <summary>
        /// Version number (0).
        /// </summary>
        public ushort Version { get; }

        /// <summary>
        /// The sub-tables, one for each supported encoding scheme and platform.
        /// </summary>
        public IReadOnlyList<ICMapSubTable> SubTables { get; }

        /// <summary>
        /// Create a new <see cref="CMapTable"/>.
        /// </summary>
        public CMapTable(ushort version, TrueTypeHeaderTable directoryTable, IReadOnlyList<ICMapSubTable> subTables)
        {
            SubTables = subTables;
            Version = version;
            DirectoryTable = directoryTable;
        }

        /// <summary>
        /// Get the glyph index for the corresponding character code.
        /// </summary>
        public bool TryGetGlyphIndex(int characterCode, out int glyphIndex)
        {
            glyphIndex = 0;

            if (SubTables.Count == 0)
            {
                return false;
            }

            var windowsMapping = default(ICMapSubTable);

            foreach (var subTable in SubTables)
            {
                glyphIndex = subTable.CharacterCodeToGlyphIndex(characterCode);

                if (glyphIndex != 0)
                {
                    return true;
                }

                if (subTable.EncodingId == 0 && subTable.PlatformId == TrueTypeCMapPlatform.Windows)
                {
                    windowsMapping = subTable;
                }
            }

            if (windowsMapping != null && characterCode >= 0 && characterCode <= 255)
            {
                // the CMap may use one of the following code ranges, so that we have to add the high byte to get the
                // mapped value
                glyphIndex = windowsMapping.CharacterCodeToGlyphIndex(characterCode + 0xF000);

                if (glyphIndex != 0)
                {
                    return true;
                }

                glyphIndex = windowsMapping.CharacterCodeToGlyphIndex(characterCode + 0xF100);

                if (glyphIndex != 0)
                {
                    return true;
                }

                glyphIndex = windowsMapping.CharacterCodeToGlyphIndex(characterCode + 0xF200);

                if (glyphIndex != 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            var startPosition = stream.Position;
            // Write cmap index.
            stream.WriteUShort(Version);
            stream.WriteUShort(SubTables.Count);

            // Write cmap encoding subtable, an index of all subtables and store the offsets to correct once written.
            var subTableIndexOffsetPositions = new long[SubTables.Count];
            for (var i = 0; i < SubTables.Count; i++)
            {
                var subTable = SubTables[i];

                stream.WriteUShort((ushort)subTable.PlatformId);
                stream.WriteUShort(subTable.EncodingId);

                subTableIndexOffsetPositions[i] = stream.Position;
                stream.WriteUInt(0);
            }

            // Write the full tables and store their actual offsets.
            var subTableActualPositions = new long[SubTables.Count];
            for (var i = 0; i < SubTables.Count; i++)
            {
                var subTable = SubTables[i];

                if (!(subTable is IWriteable writeableSubTable))
                {
                    throw new InvalidOperationException($"Cannot write subtable of type: {subTable.GetType().Name}.");
                }

                subTableActualPositions[i] = stream.Position - startPosition;

                writeableSubTable.Write(stream);
            }

            // Return to the index to fix the offset values.
            var endAt = stream.Position;

            for (var i = 0; i < subTableIndexOffsetPositions.Length; i++)
            {
                var actual = subTableActualPositions[i];
                stream.Seek(subTableIndexOffsetPositions[i], SeekOrigin.Begin);
                stream.WriteUInt(actual);
            }

            stream.Seek(endAt, SeekOrigin.Begin);
        }
    }
}
