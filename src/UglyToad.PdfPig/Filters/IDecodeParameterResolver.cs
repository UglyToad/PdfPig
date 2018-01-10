namespace UglyToad.PdfPig.Filters
{
    using ContentStream;

    internal interface IDecodeParameterResolver
    {
        PdfDictionary GetFilterParameters(PdfDictionary streamDictionary, int index);
    }
}