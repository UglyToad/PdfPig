namespace UglyToad.Pdf.Filters
{
    using ContentStream;

    public interface IDecodeParameterResolver
    {
        PdfDictionary GetFilterParameters(PdfDictionary streamDictionary, int index);
    }
}