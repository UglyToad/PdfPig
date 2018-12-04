namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    internal class Os2Table : ITable
    {
        public string Tag => TrueTypeHeaderTable.Os2;

        public TrueTypeHeaderTable DirectoryTable { get; }
    }
}
