namespace UglyToad.Pdf.Fonts.Parser
{
    using ContentStream;
    using Encodings;
    using IO;

    internal interface IEncodingReader
    {
        Encoding Read(PdfDictionary fontDictionary, IRandomAccessRead reader, bool isLenientParsing, FontDescriptor descriptor = null);
    }
}