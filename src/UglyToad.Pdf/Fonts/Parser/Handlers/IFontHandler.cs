namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using ContentStream;
    using IO;

    internal interface IFontHandler
    {
        IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing);
    }
}