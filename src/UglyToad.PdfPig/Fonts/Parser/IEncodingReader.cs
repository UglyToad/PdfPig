namespace UglyToad.PdfPig.Fonts.Parser
{
    using Encodings;
    using Tokens;

    internal interface IEncodingReader
    {
        Encoding Read(DictionaryToken fontDictionary, bool isLenientParsing, FontDescriptor descriptor = null);
    }
}