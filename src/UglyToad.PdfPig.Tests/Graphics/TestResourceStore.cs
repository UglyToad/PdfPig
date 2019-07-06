namespace UglyToad.PdfPig.Tests.Graphics
{
    using Content;
    using PdfPig.Fonts;
    using PdfPig.Tokens;

    internal class TestResourceStore : IResourceStore
    {
        public void LoadResourceDictionary(DictionaryToken dictionary, bool isLenientParsing)
        {
        }

        public IFont GetFont(NameToken name)
        {
            return null;
        }

        public StreamToken GetXObject(NameToken name)
        {
            return null;
        }

        public DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name)
        {
            return null;
        }

        public IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken, bool isLenientParsing)
        {
            return null;
        }
    }
}