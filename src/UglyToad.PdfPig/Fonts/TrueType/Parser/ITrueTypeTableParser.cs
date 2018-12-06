namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using Tables;

    internal interface ITrueTypeTableParser<out T> where T : ITable
    {
        T Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register);
    }
}