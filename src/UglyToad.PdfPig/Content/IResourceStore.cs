namespace UglyToad.PdfPig.Content
{
    using Fonts;
    using Tokens;

    internal interface IResourceStore
    {
        void LoadResourceDictionary(DictionaryToken resourceDictionary, bool isLenientParsing);

        IFont GetFont(NameToken name);

        StreamToken GetXObject(NameToken name);
    }
}