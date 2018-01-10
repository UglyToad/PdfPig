namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System.Collections.Generic;
    using CidFonts;
    using Tables;

    internal class TrueTypeFont : ICidFontProgram
    {
        public decimal Version { get; }

        public IReadOnlyDictionary<string, TrueTypeHeaderTable> Tables { get; }

        public HeaderTable HeaderTable { get; }

        public TrueTypeFont(decimal version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tables, HeaderTable headerTable)
        {
            Version = version;
            Tables = tables;
            HeaderTable = headerTable;
        }
    }
}