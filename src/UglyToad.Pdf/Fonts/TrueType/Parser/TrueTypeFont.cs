namespace UglyToad.Pdf.Fonts.TrueType.Parser
{
    using Tables;

    internal class TrueTypeFont
    {
        public decimal Version { get; }

        public HeaderTable HeaderTable { get; }

        public TrueTypeFont(decimal version, HeaderTable headerTable)
        {
            Version = version;
            HeaderTable = headerTable;
        }
    }
}