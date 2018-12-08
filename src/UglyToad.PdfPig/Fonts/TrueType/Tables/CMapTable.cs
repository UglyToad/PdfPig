namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using CMapSubTables;

    internal class CMapTable : ITable
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
    }
}
