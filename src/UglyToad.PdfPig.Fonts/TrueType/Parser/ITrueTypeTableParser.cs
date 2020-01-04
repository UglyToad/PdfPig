namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using Tables;

    internal interface ITrueTypeTableParser<out T> where T : ITrueTypeTable
    {
        T Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register);
    }
}