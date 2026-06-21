namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Graphics.Colors;
    using PdfFonts;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Colors.Icc;

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

        /// <summary>
        /// The configured ICC profile service (from <see cref="ParsingOptions.IccProfileService"/>),
        /// or <c>null</c> when ICC-based color spaces should fall back to their alternate color space.
        /// </summary>
        IIccProfileService? IccProfileService { get; }

        /// <summary>
        /// The document catalog's output intent ICC profile (document scope), resolved from the catalog's
        /// <c>/OutputIntents</c> array (<c>/DestOutputProfile</c>), or <c>null</c> when the document declares
        /// no usable output intent (or no <see cref="IccProfileService"/> is configured / output-intent
        /// colour management is disabled). Used to colour-manage the device colour spaces (DeviceCMYK /
        /// DeviceRGB / DeviceGray) per PDF/X semantics.
        /// <para>
        /// A page may override it with its own page-level <c>/OutputIntents</c> (PDF 2.0, Table 31); that
        /// override is page-scoped and is resolved per page onto the graphics state
        /// (<c>CurrentGraphicsState.OutputIntent</c>), with this document-level value as the fallback.
        /// </para>
        /// </summary>
        OutputIntent? OutputIntent { get; }
    }
}