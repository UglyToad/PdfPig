namespace UglyToad.Pdf.Fonts.TrueType.Tables
{
    internal interface ITable
    {
        string Tag { get; }

        TrueTypeFontTable DirectoryTable { get; }
    }
}
