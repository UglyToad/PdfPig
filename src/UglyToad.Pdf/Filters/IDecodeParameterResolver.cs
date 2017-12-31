namespace UglyToad.Pdf.Filters
{
    using ContentStream;

    internal interface IDecodeParameterResolver
    {
        PdfDictionary GetFilterParameters(PdfDictionary streamDictionary, int index);
    }
}