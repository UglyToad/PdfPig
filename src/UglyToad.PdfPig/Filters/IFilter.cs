namespace UglyToad.PdfPig.Filters
{
    using Tokens;

    internal interface IFilter
    {
        byte[] Decode(byte[] input, DictionaryToken streamDictionary, int filterIndex);
    }
}
