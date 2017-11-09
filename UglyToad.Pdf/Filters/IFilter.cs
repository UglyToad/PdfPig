namespace UglyToad.Pdf.Filters
{
    using ContentStream;

    public interface IFilter
    {
        byte[] Decode(byte[] input, ContentStreamDictionary streamDictionary, int filterIndex);
    }
}
