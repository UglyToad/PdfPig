namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
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
        /// Try getting the reference of the XObject corresponding to the name, without resolving the object
        /// it points at. Lets a caller identify an XObject it has already seen without re-reading its stream.
        /// </summary>
        bool TryGetXObjectReference(NameToken name, out IndirectReference reference);

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
        /// or <see langword="null"/>. When <see langword="null"/>, ICC-based color spaces will fall
        /// back to their alternate color space.
        /// </summary>
        IIccProfileService? IccProfileService { get; }

        /// <summary>
        /// The log from <see cref="ParsingOptions.Logger"/>.
        /// </summary>
        ILog Logger { get; }

        /// <summary>
        /// Every output intent declared by the document catalog (see 14.11.5, "Output intents"), in the order
        /// the <c>/OutputIntents</c> array wrote them; empty when the catalog declares none.
        /// </summary>
        IReadOnlyList<OutputIntent> DocumentOutputIntents { get; }

        /// <summary>
        /// Every output intent in effect for the content of a given page: a page-level <c>/OutputIntents</c>
        /// entry (PDF 2.0, Table 31) overrides the document catalog's <see cref="DocumentOutputIntents"/>, which is
        /// what is returned when the page carries none.
        /// </summary>
        /// <param name="pageDictionary">The page dictionary, or <c>null</c> to use the document scope.</param>
        IReadOnlyList<OutputIntent> GetPageOutputIntents(DictionaryToken? pageDictionary);

        /// <summary>
        /// The profile a page's device colours are colour-managed through, or <see langword="null"/> when
        /// they are not managed: the <see cref="OutputIntent.DestOutputProfile"/> of whichever of
        /// <see cref="GetPageOutputIntents"/> characterises the target output device.
        /// <para>
        /// Both halves of that question live here - the intents, and the <see cref="IccProfileService"/>
        /// that decides whether to honour them - so the answer is worked out once per page rather than at
        /// every colour operator.
        /// </para>
        /// </summary>
        /// <param name="pageDictionary">The page dictionary, or <c>null</c> to use the document scope.</param>
        IIccProfile? GetPageOutputIntentProfile(DictionaryToken? pageDictionary);
    }
}