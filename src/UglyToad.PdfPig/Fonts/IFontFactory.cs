namespace UglyToad.PdfPig.Fonts
{
    using Tokenization.Tokens;

    internal interface IFontFactory
    {
        IFont Get(DictionaryToken dictionary, bool isLenientParsing);
    }
}