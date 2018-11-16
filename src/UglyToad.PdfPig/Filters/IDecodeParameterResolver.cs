namespace UglyToad.PdfPig.Filters
{
    using Tokens;

    internal interface IDecodeParameterResolver
    {
        DictionaryToken GetFilterParameters(DictionaryToken streamDictionary, int index);
    }
}