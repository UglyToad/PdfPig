namespace UglyToad.PdfPig.Filters
{
    using Tokenization.Tokens;

    internal interface IDecodeParameterResolver
    {
        DictionaryToken GetFilterParameters(DictionaryToken streamDictionary, int index);
    }
}