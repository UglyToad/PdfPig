namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using Tokenization.Tokens;

    internal interface IFontHandler
    {
        IFont Generate(DictionaryToken dictionary, bool isLenientParsing);
    }
}