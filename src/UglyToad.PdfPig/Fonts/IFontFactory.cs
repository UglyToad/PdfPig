namespace UglyToad.PdfPig.Fonts
{
    using Tokens;

    internal interface IFontFactory
    {
        IFont Get(DictionaryToken dictionary, bool isLenientParsing);
    }
}