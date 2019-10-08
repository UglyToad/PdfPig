namespace UglyToad.PdfPig.Content
{
    using Fonts;
    using Tokens;

    internal interface IResourceStore
    {
        void LoadResourceDictionary(DictionaryToken resourceDictionary, bool isLenientParsing);

        IFont GetFont(NameToken name);

        StreamToken GetXObject(NameToken name);

        DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name);

        IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken, bool isLenientParsing);

        bool TryGetNamedColorSpace(NameToken name, out IToken namedColorSpace);
    }
}