namespace UglyToad.Pdf.Text
{
    public interface ITextScanner
    {
        ITextObjectComponent CurrentComponent { get; }

        bool Read();
    }
}