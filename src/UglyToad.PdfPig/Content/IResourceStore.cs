namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Graphics.Colors;
    using Graphics.Colors.Icc;
    using Logging;
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

        /// <summary>
        /// The configured ICC profile service (from <see cref="ParsingOptions.IccProfileService"/>),
        /// or <c>null</c> when ICC-based color spaces should fall back to their alternate color space.
        /// </summary>
        IIccProfileService? IccProfileService { get; }

        /// <summary>
        /// The log from <see cref="ParsingOptions.Logger"/>. Colour space parsing degrades rather than fails
        /// in a number of places - an unusable ICC profile, a mismatched <c>/Alternate</c>, a <c>/N</c> the
        /// profile disagrees with - and this is where it says so.
        /// </summary>
        ILog Logger { get; }

        /// <summary>
        /// The output intent declared by the document catalog (see 14.11.5, "Output intents"), describing the
        /// colour reproduction characteristics of the target output device. <c>null</c> only when the catalog
        /// declares none: the intent's descriptive entries are parsed whether or not an
        /// <see cref="IccProfileService"/> is configured, and without one it is
        /// <see cref="OutputIntent.DestOutputProfile"/> alone that stays null.
        /// <para>
        /// Exposed but not consumed: PdfPig does <b>not</b> render the device colour spaces (DeviceGray /
        /// DeviceRGB / DeviceCMYK) through the output intent's <c>/DestOutputProfile</c>: those colour spaces
        /// convert exactly as they do in a document with no output intent. PDFBox behaves the same way,
        /// modelling output intents without consulting them when rendering.
        /// </para>
        /// </summary>
        OutputIntent? OutputIntent { get; }

        /// <summary>
        /// The output intent in effect for the content of a given page: a page-level <c>/OutputIntents</c>
        /// entry (PDF 2.0, Table 31) overrides the document catalog's <see cref="OutputIntent"/>, which is
        /// what is returned when the page carries none. As with <see cref="OutputIntent"/>, the result
        /// describes the page and does not affect how its colours are converted.
        /// </summary>
        /// <param name="pageDictionary">The page dictionary, or <c>null</c> to use the document scope.</param>
        OutputIntent? GetPageOutputIntent(DictionaryToken? pageDictionary);
    }
}