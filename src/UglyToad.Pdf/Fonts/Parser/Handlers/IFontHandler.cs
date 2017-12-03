namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using ContentStream;
    using Pdf.Parser;

    internal interface IFontHandler
    {
        IFont Generate(PdfDictionary dictionary, ParsingArguments parsingArguments);
    }
}