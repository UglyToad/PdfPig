namespace UglyToad.PdfPig.Content
{
    using Graphics.Colors;
    using PdfFonts;
    using System.Collections.Generic;
    using Tokens;

    internal interface IResourceStore
    {
        void LoadResourceDictionary(DictionaryToken resourceDictionary, InternalParsingOptions parsingOptions);

        /// <summary>
        /// Remove any named resources and associated state for the last resource dictionary loaded.
        /// Does not affect the cached resources, just the labels associated with them.
        /// </summary>
        void UnloadResourceDictionary();

        IFont GetFont(NameToken name);

        StreamToken GetXObject(NameToken name);

        DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name);

        IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken);

        bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace);

        ColorSpaceDetails GetColorSpaceDetails(NameToken name, DictionaryToken dictionary);

        DictionaryToken GetMarkedContentPropertiesDictionary(NameToken name);

        IReadOnlyDictionary<NameToken, PatternColor> GetPatterns();

        Shading GetShading(NameToken name);
    }
}