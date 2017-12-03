namespace UglyToad.Pdf.Filters
{
    using ContentStream;

    public interface IFilter
    {
        byte[] Decode(byte[] input, PdfDictionary streamDictionary, int filterIndex);
    }
}
