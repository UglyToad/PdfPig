namespace UglyToad.Pdf.Fonts
{
    using ContentStream;
    using IO;

    internal interface IFontFactory
    {
        IFont Get(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing);
    }
}