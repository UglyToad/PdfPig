namespace UglyToad.PdfPig.Tests
{
    using IO;
    using PdfPig.ContentStream;
    using PdfPig.Cos;
    using PdfPig.Parser.Parts;

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
