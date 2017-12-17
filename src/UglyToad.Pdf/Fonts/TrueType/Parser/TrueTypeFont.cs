namespace UglyToad.Pdf.Fonts.TrueType.Parser
{
    using System.Collections.Generic;
    using Tables;

    internal class TrueTypeFont
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