namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using Parser;
    using Tables;

    internal class TrueTypeFont : ICidFontProgram
    {
        public decimal Version { get; }

        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        public HeaderTable HeaderTable { get; }
        public CMapTable CMapTable { get; }
        public GlyphDataTable GlyphTable { get; }

        public TrueTypeFont(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister)
        {
            if (tableRegister == null)
            {
                throw new ArgumentNullException(nameof(tableRegister));
            }

            Version = version;
            TableHeaders = tableHeaders;
            HeaderTable = tableRegister.HeaderTable;
            CMapTable = tableRegister.CMapTable;
            GlyphTable = tableRegister.GlyphDataTable;
        }
    }
}