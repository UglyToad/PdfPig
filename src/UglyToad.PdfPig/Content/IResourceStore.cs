namespace UglyToad.PdfPig.Content
{
    using Graphics.Colors;
    using PdfFonts;
    using Tokens;

    /// <summary>
    /// ResourceStore interface.
    /// </summary>
    public interface IResourceStore
    {
        /// <summary>
        /// Load Resource Dictionary.
        /// </summary>
        /// <param name="resourceDictionary"></param>
        void LoadResourceDictionary(DictionaryToken resourceDictionary);

        /// <summary>
        /// Remove any named resources and associated state for the last resource dictionary loaded.
        /// Does not affect the cached resources, just the labels associated with them.
        /// </summary>
        void UnloadResourceDictionary();

        /// <summary>
        /// GetFont
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IFont GetFont(NameToken name);

        /// <summary>
        /// GetXObject
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        StreamToken GetXObject(NameToken name);

        /// <summary>
        /// GetExtendedGraphicsStateDictionary
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name);

        /// <summary>
        /// GetFontDirectly
        /// </summary>
        /// <param name="fontReferenceToken"></param>
        /// <returns></returns>
        IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken);

        /// <summary>
        /// TryGetNamedColorSpace
        /// </summary>
        /// <param name="name"></param>
        /// <param name="namedColorSpace"></param>
        /// <returns></returns>
        bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace);

        /// <summary>
        /// GetMarkedContentPropertiesDictionary
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        DictionaryToken GetMarkedContentPropertiesDictionary(NameToken name);
    }
}