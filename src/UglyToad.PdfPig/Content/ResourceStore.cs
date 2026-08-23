namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Core;
    using Filters;
    using Graphics.Colors;
    using Parser.Parts;
    using PdfFonts;
    using Tokenization.Scanner;
    using Tokens;
    using Graphics.Colors.Icc;
    using Logging;
    using Util;

    internal sealed class ResourceStore : IResourceStore
    {
        private readonly IPdfTokenScanner scanner;
        private readonly IFontFactory fontFactory;
        private readonly ILookupFilterProvider filterProvider;
        private readonly ParsingOptions parsingOptions;

        private readonly Dictionary<IndirectReference, IFont> loadedFonts = new Dictionary<IndirectReference, IFont>();
        private readonly Dictionary<NameToken, IFont> loadedDirectFonts = new Dictionary<NameToken, IFont>();
        private readonly StackDictionary<NameToken, IndirectReference> currentFontState = new StackDictionary<NameToken, IndirectReference>();
        private readonly StackDictionary<NameToken, IndirectReference> currentXObjectState = new StackDictionary<NameToken, IndirectReference>();

        private readonly StackDictionary<NameToken, DictionaryToken> extendedGraphicsStates = new StackDictionary<NameToken, DictionaryToken>();

        private readonly StackDictionary<NameToken, ResourceColorSpace> namedColorSpaces = new StackDictionary<NameToken, ResourceColorSpace>();
        private readonly Dictionary<NameToken, ColorSpaceDetails> loadedNamedColorSpaceDetails = new Dictionary<NameToken, ColorSpaceDetails>();
        private readonly Dictionary<(NameToken? Name, IToken ColorSpace), ColorSpaceDetails> loadedColorSpaceDetailsCache = new Dictionary<(NameToken?, IToken), ColorSpaceDetails>();

        // NOT cleared per resource dictionary: it is keyed by the profile stream's indirect reference (unique document-wide)
        private readonly IccProfileCache iccProfileByteCache = new IccProfileCache();

        private readonly StackDictionary<NameToken, DictionaryToken> markedContentProperties = new StackDictionary<NameToken, DictionaryToken>();

        private readonly StackDictionary<NameToken, Shading> shadingsProperties = new StackDictionary<NameToken, Shading>();

        private readonly StackDictionary<NameToken, PatternColor> patternsProperties = new StackDictionary<NameToken, PatternColor>();
        
        private readonly Dictionary<DictionaryToken, ResolvedResources> resolvedResources = new();

        // 8.6.5.6: while a DefaultGray/RGB/CMYK substitution is being resolved, any device colour space
        // encountered inside the substitute's own definition refers to the genuine device space and shall
        // NOT be substituted again. This mirrors PDFBox's 'wasDefault' flag and breaks otherwise-infinite
        // recursion for a default whose definition references the same device space, e.g.
        // /DefaultCMYK [ /DeviceN [ ... ] /DeviceCMYK <tint function> ].
        private bool isResolvingDefaultSubstitute;

        private (NameToken? name, IFont? font) lastLoadedFont;

        public IIccProfileService? IccProfileService => parsingOptions.IccProfileService;

        public ILog Logger => parsingOptions.Logger;

        private readonly Lazy<IReadOnlyList<OutputIntent>> outputIntents;

        public IReadOnlyList<OutputIntent> DocumentOutputIntents => outputIntents.Value;

        private readonly Dictionary<IndirectReference, IReadOnlyList<OutputIntent>> pageOutputIntents = new();

        public ResourceStore(IPdfTokenScanner scanner,
            IFontFactory fontFactory,
            ILookupFilterProvider filterProvider,
            DictionaryToken? catalogDictionary,
            ParsingOptions parsingOptions)
        {
            this.scanner = scanner;
            this.fontFactory = fontFactory;
            this.filterProvider = filterProvider;
            this.parsingOptions = parsingOptions;
            this.outputIntents = catalogDictionary is null
                ? new Lazy<IReadOnlyList<OutputIntent>>(() => [])
                : new Lazy<IReadOnlyList<OutputIntent>>(() => OutputIntentParser.CreateAll(catalogDictionary,
                    scanner, filterProvider, parsingOptions.IccProfileService, iccProfileByteCache, Logger));
        }

        /// <inheritdoc/>
        public IReadOnlyList<OutputIntent> GetPageOutputIntents(DictionaryToken? pageDictionary)
        {
            if (pageDictionary is null || !pageDictionary.TryGet(NameToken.OutputIntents, out var outputIntentsToken))
            {
                return DocumentOutputIntents;
            }

            if (outputIntentsToken is not IndirectReferenceToken reference)
            {
                var parsed = ParsePageOutputIntents(pageDictionary);
                return parsed.Count > 0 ? parsed : DocumentOutputIntents;
            }

            if (!pageOutputIntents.TryGetValue(reference.Data, out var cached))
            {
                cached = ParsePageOutputIntents(pageDictionary);
                pageOutputIntents[reference.Data] = cached;
            }

            // A page whose own array yielded nothing usable still sits inside the document's declaration.
            return cached.Count > 0 ? cached : DocumentOutputIntents;
        }

        /// <inheritdoc/>
        public IIccProfile? GetPageOutputIntentProfile(DictionaryToken? pageDictionary)
        {
            var service = parsingOptions.IccProfileService;

            return service is null
                ? null
                : OutputIntentColorManagement.GetDeviceProfile(GetPageOutputIntents(pageDictionary), service);
        }

        private IReadOnlyList<OutputIntent> ParsePageOutputIntents(DictionaryToken pageDictionary)
        {
            return OutputIntentParser.CreateAll(pageDictionary, scanner, filterProvider,
                parsingOptions.IccProfileService, iccProfileByteCache, Logger);
        }

        public void LoadResourceDictionary(DictionaryToken resourceDictionary)
        {
            lastLoadedFont = (null, null);
            loadedNamedColorSpaceDetails.Clear();
            loadedColorSpaceDetailsCache.Clear();

            if (!resolvedResources.TryGetValue(resourceDictionary, out var resolved))
            {
                resolved = ResolveResources(resourceDictionary);
                resolvedResources[resourceDictionary] = resolved;
            }

            namedColorSpaces.Push(resolved.NamedColorSpaces);
            currentFontState.Push(resolved.Fonts);
            currentXObjectState.Push(resolved.XObjects);
            extendedGraphicsStates.Push(resolved.ExtendedGraphicsStates);
            markedContentProperties.Push(resolved.MarkedContentProperties);
            shadingsProperties.Push();
            patternsProperties.Push();

            // Fonts given inline rather than by indirect reference are keyed by name in a store shared
            // across resource dictionaries, so their binding is re-established on every load. The font
            // itself was parsed when this dictionary was first resolved.
            for (var i = 0; i < resolved.DirectFonts.Count; i++)
            {
                var directFont = resolved.DirectFonts[i];
                loadedDirectFonts[directFont.Name] = directFont.Font;
            }

            // Patterns and shadings resolve their colour spaces through the resource stack, so unlike the
            // categories above their result depends on the levels below this one and cannot be cached
            // against the resource dictionary alone.
            LoadPatterns(resourceDictionary);
            LoadShadings(resourceDictionary);
        }

        private ResolvedResources ResolveResources(DictionaryToken resourceDictionary)
        {
            var fonts = new Dictionary<NameToken, IndirectReference>();
            var directFonts = new List<(NameToken Name, IFont Font)>();
            var xObjects = new Dictionary<NameToken, IndirectReference>();
            var extendedGraphicsStateDictionaries = new Dictionary<NameToken, DictionaryToken>();
            var colorSpaces = new Dictionary<NameToken, ResourceColorSpace>();
            var properties = new Dictionary<NameToken, DictionaryToken>();

            if (resourceDictionary.TryGet(NameToken.Font, out var fontBase))
            {
                var fontDictionary = DirectObjectFinder.Get<DictionaryToken>(fontBase, scanner);

                LoadFontDictionary(fontDictionary, fonts, directFonts);
            }

            if (resourceDictionary.TryGet(NameToken.Xobject, out var xobjectBase))
            {
                var xobjectDictionary = DirectObjectFinder.Get<DictionaryToken>(xobjectBase, scanner);

                foreach (var pair in xobjectDictionary.Data)
                {
                    if (pair.Value is NullToken)
                    {
                        continue;
                    }

                    if (!(pair.Value is IndirectReferenceToken reference))
                    {
                        throw new InvalidOperationException($"Expected the XObject dictionary value for key /{pair.Key} to be an indirect reference, instead got: {pair.Value}.");
                    }

                    xObjects[NameToken.Create(pair.Key)] = reference.Data;
                }
            }

            if (resourceDictionary.TryGet(NameToken.ExtGState, scanner, out DictionaryToken? extGStateDictionaryToken))
            {
                foreach (var pair in extGStateDictionaryToken.Data)
                {
                    var name = NameToken.Create(pair.Key);
                    var state = DirectObjectFinder.Get<DictionaryToken>(pair.Value, scanner);

                    extendedGraphicsStateDictionaries[name] = state;
                }
            }

            if (resourceDictionary.TryGet(NameToken.ColorSpace, scanner, out DictionaryToken? colorSpaceDictionary))
            {
                foreach (var nameColorSpacePair in colorSpaceDictionary.Data)
                {
                    var name = NameToken.Create(nameColorSpacePair.Key);

                    if (DirectObjectFinder.TryGet(nameColorSpacePair.Value, scanner, out NameToken? colorSpaceName))
                    {
                        colorSpaces[name] = new ResourceColorSpace(colorSpaceName);
                    }
                    else if (DirectObjectFinder.TryGet(nameColorSpacePair.Value, scanner, out ArrayToken? colorSpaceArray))
                    {
                        if (colorSpaceArray.Length == 0)
                        {
                            throw new PdfDocumentFormatException($"Empty ColorSpace array encountered in page resource dictionary: {resourceDictionary}.");
                        }

                        var first = colorSpaceArray.Data[0];

                        if (!(first is NameToken arrayNamedColorSpace))
                        {
                            throw new PdfDocumentFormatException($"Invalid ColorSpace array encountered in page resource dictionary: {colorSpaceArray}.");
                        }

                        colorSpaces[name] = new ResourceColorSpace(arrayNamedColorSpace, colorSpaceArray);
                    }
                    else if (parsingOptions.UseLenientParsing &&
                             DirectObjectFinder.TryGet(nameColorSpacePair.Value, scanner, out DictionaryToken? dict) &&
                             dict.TryGet(NameToken.ColorSpace, scanner, out NameToken? csName))
                    {
                        // See issue #1061
                        colorSpaces[name] = new ResourceColorSpace(csName);
                    }
                    else
                    {
                        throw new PdfDocumentFormatException($"Invalid ColorSpace token encountered in page resource dictionary: {nameColorSpacePair.Value}.");
                    }
                }
            }

            if (resourceDictionary.TryGet(NameToken.Properties, scanner, out DictionaryToken? markedContentPropertiesList))
            {
                foreach (var pair in markedContentPropertiesList.Data)
                {
                    var key = NameToken.Create(pair.Key);

                    if (!DirectObjectFinder.TryGet(pair.Value, scanner, out DictionaryToken? namedProperties))
                    {
                        continue;
                    }

                    properties[key] = namedProperties;
                }
            }

            return new ResolvedResources(fonts, directFonts, xObjects, extendedGraphicsStateDictionaries, colorSpaces, properties);
        }

        private void LoadPatterns(DictionaryToken resourceDictionary)
        {
            if (!resourceDictionary.TryGet(NameToken.Pattern, scanner, out DictionaryToken? patternDictionary))
            {
                return;
            }

            // NB: in PDF, all patterns shall be local to the context in which they are defined.
            foreach (var namePatternPair in patternDictionary.Data)
            {
                var name = NameToken.Create(namePatternPair.Key);
                patternsProperties[name] = PatternParser.Create(namePatternPair.Value, scanner, this, filterProvider);
            }
        }

        private void LoadShadings(DictionaryToken resourceDictionary)
        {
            if (!resourceDictionary.TryGet(NameToken.Shading, scanner, out DictionaryToken? shadingList))
            {
                return;
            }

            foreach (var pair in shadingList.Data)
            {
                var key = NameToken.Create(pair.Key);
                if (DirectObjectFinder.TryGet(pair.Value, scanner, out DictionaryToken? namedPropertiesDictionary))
                {
                    shadingsProperties[key] = ShadingParser.Create(namedPropertiesDictionary, scanner, this, filterProvider);
                }
                else if (DirectObjectFinder.TryGet(pair.Value, scanner, out StreamToken? namedPropertiesStream))
                {
                    // Shading types 4 to 7 shall be defined by a stream containing descriptive data characterizing
                    // the shading's gradient fill.
                    shadingsProperties[key] = ShadingParser.Create(namedPropertiesStream, scanner, this, filterProvider);
                }
                else
                {
                    throw new NotImplementedException("Shading");
                }
            }
        }

        public void UnloadResourceDictionary()
        {
            lastLoadedFont = (null, null);
            loadedNamedColorSpaceDetails.Clear();
            loadedColorSpaceDetailsCache.Clear();
            currentFontState.Pop();
            currentXObjectState.Pop();
            namedColorSpaces.Pop();
            extendedGraphicsStates.Pop();
            markedContentProperties.Pop();
            shadingsProperties.Pop();
            patternsProperties.Pop();
        }

        private void LoadFontDictionary(DictionaryToken fontDictionary,
            Dictionary<NameToken, IndirectReference> fonts,
            List<(NameToken Name, IFont Font)> directFonts)
        {
            lastLoadedFont = (null, null);

            foreach (var pair in fontDictionary.Data)
            {
                if (pair.Value is IndirectReferenceToken objectKey)
                {
                    var reference = objectKey.Data;

                    fonts[NameToken.Create(pair.Key)] = reference;

                    if (loadedFonts.ContainsKey(reference))
                    {
                        continue;
                    }

                    var fontObject = DirectObjectFinder.Get<DictionaryToken>(objectKey, scanner);

                    if (fontObject is null)
                    {
                        //This is a valid use case
                        continue;
                    }

                    try
                    {
                        var loadedFont = fontFactory.Get(fontObject);
                        // Stamp the font dictionary's indirect reference so consumers can
                        // distinguish same-named fonts (e.g. two subsets of one typeface
                        // embedded without unique subset prefixes). See FontDetails.FontDictionaryReference.
                        loadedFont.Details?.SetFontDictionaryReference(reference);
                        loadedFonts[reference] = loadedFont;
                    }
                    catch
                    {
                        if (!parsingOptions.SkipMissingFonts)
                        {
                            throw;
                        }
                    }
                }
                else if (pair.Value is DictionaryToken fd)
                {
                    var name = NameToken.Create(pair.Key);
                    var font = fontFactory.Get(fd);

                    directFonts.Add((name, font));
                    loadedDirectFonts[name] = font;
                }
                else
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// The result of expanding a resource dictionary, used for caching.
        /// </summary>
        private sealed class ResolvedResources
        {
            public Dictionary<NameToken, IndirectReference> Fonts { get; }

            /// <summary>
            /// Fonts written inline in the resource dictionary rather than referenced indirectly. These are
            /// held by name in a store shared across resource dictionaries so the binding, unlike the parsed
            /// font, has to be re-applied on every load.
            /// </summary>
            public IReadOnlyList<(NameToken Name, IFont Font)> DirectFonts { get; }

            public Dictionary<NameToken, IndirectReference> XObjects { get; }

            public Dictionary<NameToken, DictionaryToken> ExtendedGraphicsStates { get; }

            public Dictionary<NameToken, ResourceColorSpace> NamedColorSpaces { get; }

            public Dictionary<NameToken, DictionaryToken> MarkedContentProperties { get; }

            public ResolvedResources(
                Dictionary<NameToken, IndirectReference> fonts,
                IReadOnlyList<(NameToken Name, IFont Font)> directFonts,
                Dictionary<NameToken, IndirectReference> xObjects,
                Dictionary<NameToken, DictionaryToken> extendedGraphicsStates,
                Dictionary<NameToken, ResourceColorSpace> namedColorSpaces,
                Dictionary<NameToken, DictionaryToken> markedContentProperties)
            {
                Fonts = fonts;
                DirectFonts = directFonts;
                XObjects = xObjects;
                ExtendedGraphicsStates = extendedGraphicsStates;
                NamedColorSpaces = namedColorSpaces;
                MarkedContentProperties = markedContentProperties;
            }
        }

        public IFont? GetFont(NameToken name)
        {
            if (lastLoadedFont.name == name)
            {
                return lastLoadedFont.font;
            }

            IFont? font;
            if (currentFontState.TryGetValue(name, out var reference))
            {
                loadedFonts.TryGetValue(reference, out font);
            }
            else if (!loadedDirectFonts.TryGetValue(name, out font))
            {
                return null;
            }

            lastLoadedFont = (name, font);

            return font;
        }

        public IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken)
        {
            lastLoadedFont = (null, null);

            if (!DirectObjectFinder.TryGet(fontReferenceToken, scanner, out DictionaryToken? fontDictionaryToken))
            {
                throw new PdfDocumentFormatException($"The requested font reference token {fontReferenceToken} wasn't a font.");
            }

            var font = fontFactory.Get(fontDictionaryToken);

            font.Details?.SetFontDictionaryReference(fontReferenceToken.Data);

            return font;
        }

        public bool TryGetNamedColorSpace(NameToken? name, out ResourceColorSpace namedToken)
        {
            namedToken = default(ResourceColorSpace);

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!namedColorSpaces.TryGetValue(name, out var colorSpaceName))
            {
                return false;
            }

            namedToken = colorSpaceName;

            return true;
        }

        public ColorSpaceDetails GetColorSpaceDetails(NameToken? name, DictionaryToken? dictionary)
        {
            dictionary ??= new DictionaryToken(new Dictionary<NameToken, IToken>());

            if (!TryGetCacheColorSpaceDefinition(dictionary, out IToken? colorSpaceToken))
            {
                return GetColorSpaceDetailsInternal(name, dictionary);
            }

            var key = (name, colorSpaceToken);
            if (loadedColorSpaceDetailsCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var parsed = GetColorSpaceDetailsInternal(name, dictionary);
            loadedColorSpaceDetailsCache[key] = parsed;
            return parsed;
        }

        private bool TryGetCacheColorSpaceDefinition(DictionaryToken dictionary, [NotNullWhen(true)] out IToken? colorSpaceToken)
        {
            colorSpaceToken = null;

            // While a DefaultGray/RGB/CMYK substitute is being resolved the same colour space object can
            // legitimately parse to a different result, so bypass the cache entirely.
            if (isResolvingDefaultSubstitute)
            {
                return false;
            }

            // We rely on the color space definition for caching.
            if (!dictionary.TryGet(NameToken.ColorSpace, out colorSpaceToken) &&
                !dictionary.TryGet(NameToken.Cs, out colorSpaceToken))
            {
                return false;
            }

            // We do not cache stencil-mask color spaces as they do not rely on color space definition.
            // Stencil color spaces are created when the dictionary contains `ImageMask` or `Im` or if
            // a filter is CcittFaxDecodeFilter.
            if (dictionary.ContainsKey(NameToken.ImageMask) || dictionary.ContainsKey(NameToken.Im))
            {
                return false;
            }

            if ((dictionary.ContainsKey(NameToken.Filter) || dictionary.ContainsKey(NameToken.F)) &&
                filterProvider.GetFilters(dictionary, scanner).OfType<CcittFaxDecodeFilter>().Any())
            {
                return false;
            }

            // NB: If the colorSpaceToken is an indirect reference, we do not resolve it.
            // This could change, fine for now

            return true;
        }

        private ColorSpaceDetails GetColorSpaceDetailsInternal(NameToken? name, DictionaryToken dictionary)
        {
            // Null color space for images
            if (name is null)
            {
                return ColorSpaceDetailsParser.GetColorSpaceDetails(null, dictionary, scanner, this,
                    filterProvider, iccProfileByteCache);
            }

            if (name.TryMapToColorSpace(out ColorSpace colorSpaceActual))
            {
                if (TryGetDefaultSubstitute(colorSpaceActual, out NameToken? substituteName))
                {
                    return ResolveDefaultSubstitute(colorSpaceActual, substituteName, dictionary);
                }

                return ColorSpaceDetailsParser.GetColorSpaceDetails(colorSpaceActual, dictionary, scanner, this,
                    filterProvider, iccProfileByteCache);
            }

            // Named color spaces
            if (loadedNamedColorSpaceDetails.TryGetValue(name, out ColorSpaceDetails? csdLoaded))
            {
                return csdLoaded;
            }

            if (TryGetNamedColorSpace(name, out ResourceColorSpace namedColorSpace) &&
                namedColorSpace.Name.TryMapToColorSpace(out ColorSpace mapped))
            {
                if (namedColorSpace.Data is null)
                {
                    return ColorSpaceDetailsParser.GetColorSpaceDetails(mapped, dictionary, scanner, this,
                        filterProvider, iccProfileByteCache);
                }
                
                if (namedColorSpace.Data is ArrayToken array)
                {
                    var csd = ColorSpaceDetailsParser.GetColorSpaceDetails(mapped, dictionary.With(NameToken.ColorSpace, array), scanner, this,
                        filterProvider, iccProfileByteCache);
                    loadedNamedColorSpaceDetails[name] = csd;
                    return csd;
                }
            }

            throw new InvalidOperationException($"Could not find color space for token '{name}'.");
        }

        public ColorSpaceDetails GetDeviceColorSpaceDetails(ColorSpace deviceColorSpace)
        {
            // 8.6.5.6: a directly selected device colour space is remapped to its DefaultGray/RGB/CMYK
            // substitute when a valid one is present in the current resources; otherwise the device space
            // singleton is returned.
            if (TryGetDefaultSubstitute(deviceColorSpace, out NameToken? substituteName))
            {
                return ResolveDefaultSubstitute(deviceColorSpace, substituteName, null);
            }

            return GetDeviceColorSpaceSingleton(deviceColorSpace);
        }

        private static ColorSpaceDetails GetDeviceColorSpaceSingleton(ColorSpace deviceColorSpace)
        {
            return deviceColorSpace switch
            {
                ColorSpace.DeviceGray => DeviceGrayColorSpaceDetails.Instance,
                ColorSpace.DeviceRGB => DeviceRgbColorSpaceDetails.Instance,
                ColorSpace.DeviceCMYK => DeviceCmykColorSpaceDetails.Instance,
                _ => throw new ArgumentOutOfRangeException(nameof(deviceColorSpace),
                    deviceColorSpace,
                    "Expected a device colour space (DeviceGray, DeviceRGB or DeviceCMYK).")
            };
        }

        /// <summary>
        /// Get the default substitute color space.
        /// </summary>
        /// <param name="requested">The device colour space being substituted for, which stands if the substitute turns out to be unusable.</param>
        /// <param name="substituteName"></param>
        /// <param name="dictionary"></param>
        private ColorSpaceDetails ResolveDefaultSubstitute(ColorSpace requested, NameToken substituteName,
            DictionaryToken? dictionary)
        {
            ColorSpaceDetails substitute;

            isResolvingDefaultSubstitute = true;
            try
            {
                substitute = GetColorSpaceDetails(substituteName, dictionary);
            }
            finally
            {
                isResolvingDefaultSubstitute = false;
            }

            ColorSpaceDetails device = GetDeviceColorSpaceSingleton(requested);

            if (substitute is UnsupportedColorSpaceDetails or PatternColorSpaceDetails)
            {
                // If substitute failed to parse, then we revert back to device cs (G/RGB/CMYK).
                // Pattern is also substituted here because 8.6.5.6 forbids it as a default, and its
                // NumberOfColorComponents throws.
                Logger.Warn($"The {substituteName} colour space in the current resources cannot be used as a default " +
                            $"colour space; using {requested} itself instead.");

                return device;
            }

            if (substitute.NumberOfColorComponents != device.NumberOfColorComponents)
            {
                Logger.Warn($"The {substituteName} colour space in the current resources takes " +
                            $"{substitute.NumberOfColorComponents} components where {requested} has " +
                            $"{device.NumberOfColorComponents}; ignoring it and using {requested} itself instead.");

                return device;
            }

            return substitute;
        }

        private bool TryGetDefaultSubstitute(ColorSpace requested, [NotNullWhen(true)] out NameToken? substituteName)
        {
            substituteName = null;

            // Don't substitute while already resolving a default, the device space is the genuine one
            // (see isResolvingDefaultSubstitute).
            if (isResolvingDefaultSubstitute)
            {
                return false;
            }

            NameToken? candidate = requested switch
            {
                ColorSpace.DeviceGray => NameToken.DefaultGray,
                ColorSpace.DeviceRGB => NameToken.DefaultRgb,
                ColorSpace.DeviceCMYK => NameToken.DefaultCmyk,
                _ => null
            };

            // 8.6.5.6: any colour space other than a Lab, Indexed, or Pattern colour space may be used as a
            // default. Reject those families so an invalid default falls back to the genuine device space.
            if (candidate is not null &&
                TryGetNamedColorSpace(candidate, out ResourceColorSpace substitute) &&
                substitute.Name.TryMapToColorSpace(out ColorSpace substituteColorSpace) &&
                substituteColorSpace is not ColorSpace.Lab and not ColorSpace.Indexed and not ColorSpace.Pattern)
            {
                substituteName = candidate;
                return true;
            }

            return false;
        }

        public bool TryGetXObject(NameToken name, [NotNullWhen(true)] out StreamToken? stream)
        {
            stream = null;
            if (!currentXObjectState.TryGetValue(name, out var indirectReference))
            {
                return false;
            }

            return DirectObjectFinder.TryGet(new IndirectReferenceToken(indirectReference), scanner, out stream);
        }

        public bool TryGetXObjectReference(NameToken name, out IndirectReference reference)
        {
            return currentXObjectState.TryGetValue(name, out reference);
        }

        public DictionaryToken? GetExtendedGraphicsStateDictionary(NameToken name)
        {
            if (parsingOptions.UseLenientParsing)
            {
                if (extendedGraphicsStates.TryGetValue(name, out var dictToken))
                {
                    return dictToken;
                }

                Logger.Error($"The graphic state dictionary does not contain the key '{name}'.");
                return null;
            }

            return extendedGraphicsStates[name];
        }

        public DictionaryToken? GetMarkedContentPropertiesDictionary(NameToken name)
        {
            return markedContentProperties.TryGetValue(name, out var result) ? result : null;
        }

        public Shading GetShading(NameToken name)
        {
            return shadingsProperties[name];
        }

        public IReadOnlyDictionary<NameToken, PatternColor> GetPatterns()
        {
            return patternsProperties.Flatten();
        }
    }
}
