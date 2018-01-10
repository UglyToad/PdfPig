namespace UglyToad.PdfPig.Filters
{
    using ContentStream;

    internal interface IFilter
    {
        byte[] Decode(byte[] input, PdfDictionary streamDictionary, int filterIndex);
    }
}
