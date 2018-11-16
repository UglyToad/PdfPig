namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using Tokens;

    internal interface IFontHandler
    {
        IFont Generate(DictionaryToken dictionary, bool isLenientParsing);
    }
}