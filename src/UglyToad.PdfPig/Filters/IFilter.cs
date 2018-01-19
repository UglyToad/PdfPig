namespace UglyToad.PdfPig.Filters
{
    using Tokenization.Tokens;

    internal interface IFilter
    {
        byte[] Decode(byte[] input, DictionaryToken streamDictionary, int filterIndex);
    }
}
