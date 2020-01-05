namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using Fonts.Encodings;
    using Tokens;

    internal interface IEncodingReader
    {
        Encoding Read(DictionaryToken fontDictionary, bool isLenientParsing, FontDescriptor descriptor = null,
            Encoding fontEncoding = null);
    }
}