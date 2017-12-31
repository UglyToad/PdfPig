namespace UglyToad.Pdf.Tests
{
    using IO;
    using Pdf.ContentStream;
    using Pdf.Cos;
    using Pdf.Parser.Parts;

    internal class TestDictionaryParser : IDictionaryParser
    {
        public PdfDictionary Parse(IRandomAccessRead reader, IBaseParser baseParser, CosObjectPool pool)
        {
            return new PdfDictionary();
        }
    }

    internal class TestBaseParser : IBaseParser
    {
        public CosBase Parse(IRandomAccessRead reader, CosObjectPool pool)
        {
            return CosNull.Null;
        }
    }
}
