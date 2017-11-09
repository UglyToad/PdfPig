namespace UglyToad.Pdf.Filters
{
    using ContentStream;

    public interface IDecodeParameterResolver
    {
        ContentStreamDictionary GetFilterParameters(ContentStreamDictionary streamDictionary, int index);
    }
}