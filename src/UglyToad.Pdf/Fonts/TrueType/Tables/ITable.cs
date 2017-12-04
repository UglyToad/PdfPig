namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    internal interface ITable
    {
        string Tag { get; }

        TrueTypeHeaderTable DirectoryTable { get; }
    }
}
