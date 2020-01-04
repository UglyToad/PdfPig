namespace UglyToad.PdfPig.PdfFonts
{
    using Tokens;

    internal interface IFontFactory
    {
        IFont Get(DictionaryToken dictionary, bool isLenientParsing);
    }
}