namespace UglyToad.Pdf.Fonts.TrueType.Parser
{
    using Tables;

    internal interface ITrueTypeTableParser
    {
        string Tag { get; }

        ITable Parse(TrueTypeDataBytes data, TrueTypeFontTable table);
    }
}