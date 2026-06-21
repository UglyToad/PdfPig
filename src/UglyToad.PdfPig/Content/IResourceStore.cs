namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Graphics.Colors;
    using PdfFonts;
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
        IFont? GetFont(NameToken name);

        /// <summary>
        /// Try getting the XObject corresponding to the name.
        /// </summary>
        bool TryGetXObject(NameToken name, [NotNullWhen(true)] out StreamToken? stream);

        /// <summary>
        /// Get the extended graphics state dictionary corresponding to the name.
        /// </summary>
        DictionaryToken? GetExtendedGraphicsStateDictionary(NameToken name);

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
        ColorSpaceDetails GetColorSpaceDetails(NameToken? name, DictionaryToken? dictionary);

        /// <summary>
        /// Get the colour space details for a device colour space selected directly (for example by the
        /// <c>g</c> / <c>rg</c> / <c>k</c> operators), applying the <c>DefaultGray</c> / <c>DefaultRGB</c> /
        /// <c>DefaultCMYK</c> substitution from the current resource dictionary when present (PDF 2.0,
        /// 8.6.5.6 "Default colour spaces"). Returns the device colour space itself when no matching
        /// default colour space is defined.
        /// </summary>
        ColorSpaceDetails GetDeviceColorSpaceDetails(ColorSpace deviceColorSpace);

        /// <summary>
        /// Get the marked content properties dictionary corresponding to the name.
        /// </summary>
        DictionaryToken? GetMarkedContentPropertiesDictionary(NameToken name);

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