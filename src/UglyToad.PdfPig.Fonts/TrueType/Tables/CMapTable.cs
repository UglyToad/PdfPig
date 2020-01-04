namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using CMapSubTables;
    using Core;

    internal class CMapTable : ITrueTypeTable, IWriteable
    {
        public IReadOnlyList<ICMapSubTable> SubTables { get; }

        public string Tag => TrueTypeHeaderTable.Cmap;

        public int Version { get; }

        public TrueTypeHeaderTable DirectoryTable { get; }

        public CMapTable(int version, TrueTypeHeaderTable directoryTable, IReadOnlyList<ICMapSubTable> subTables)
        {
            SubTables = subTables;
            Version = version;
            DirectoryTable = directoryTable;
        }

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
