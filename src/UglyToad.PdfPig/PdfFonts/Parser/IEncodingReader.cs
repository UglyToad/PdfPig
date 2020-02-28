namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using Fonts.Encodings;
    using Tokens;

    internal interface IEncodingReader
    {
        Encoding Read(DictionaryToken fontDictionary, FontDescriptor descriptor = null,
            Encoding fontEncoding = null);
    }
}