namespace UglyToad.PdfPig.Content
{
    using Graphics.Colors;
    using PdfFonts;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Resource store.
    /// </summary>
    public interface IResourceStore
    {
        /// <summary>
        /// Load the resource dictionary.
        /// </summary>
        void LoadResourceDictionary(DictionaryToken resourceDictionary);

        /// <summary>
        /// Remove any named resources and associated state for the last resource dictionary loaded.
        /// Does not affect the cached resources, just the labels associated with them.
        /// </summary>
        void UnloadResourceDictionary();

        /// <summary>
        /// Get the font corresponding to the name.
        /// </summary>
        IFont GetFont(NameToken name);

        /// <summary>
        /// Try getting the XObject corresponding to the name.
        /// </summary>
        bool TryGetXObject(NameToken name, out StreamToken stream);

        /// <summary>
        /// Get the extended graphics state dictionary corresponding to the name.
        /// </summary>
        DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name);

        /// <summary>
        /// Get the font from the <see cref="IndirectReferenceToken"/>.
        /// </summary>
        IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken);

        /// <summary>
        /// Get the named color space by its name.
        /// </summary>
        bool TryGetNamedColorSpace(NameToken name, out ResourceColorSpace namedColorSpace);

        /// <summary>
        /// Get the color space details corresponding to the name.
        /// </summary>
        ColorSpaceDetails GetColorSpaceDetails(NameToken name, DictionaryToken dictionary);

        /// <summary>
        /// Get the marked content properties dictionary corresponding to the name.
        /// </summary>
        DictionaryToken GetMarkedContentPropertiesDictionary(NameToken name);

        /// <summary>
        /// Get all <see cref="PatternColor"/> as a dictionary. Keys are the <see cref="PatternColor"/> names.
        /// </summary>
        IReadOnlyDictionary<NameToken, PatternColor> GetPatterns();

        /// <summary>
        /// Get the shading corresponding to the name.
        /// </summary>
        Shading GetShading(NameToken name);
    }
}