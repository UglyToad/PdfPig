namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Graphics.Colors;
    using Parser.Parts;
    using PdfFonts;
    using Tokenization.Scanner;
    using Tokens;
    using Filters;
    using Util;

    internal class ResourceStore : IResourceStore
    {
        private readonly IPdfTokenScanner scanner;
        private readonly IFontFactory fontFactory;
        private readonly ILookupFilterProvider filterProvider;
        private readonly ParsingOptions parsingOptions;

        private readonly Dictionary<IndirectReference, IFont> loadedFonts = new Dictionary<IndirectReference, IFont>();
        private readonly Dictionary<NameToken, IFont> loadedDirectFonts = new Dictionary<NameToken, IFont>();
        private readonly StackDictionary<NameToken, IndirectReference> currentFontState = new StackDictionary<NameToken, IndirectReference>();
        private readonly StackDictionary<NameToken, IndirectReference> currentXObjectState = new StackDictionary<NameToken, IndirectReference>();

        private readonly Dictionary<NameToken, DictionaryToken> extendedGraphicsStates = new Dictionary<NameToken, DictionaryToken>();

        private readonly StackDictionary<NameToken, ResourceColorSpace> namedColorSpaces = new StackDictionary<NameToken, ResourceColorSpace>();
        private readonly Dictionary<NameToken, ColorSpaceDetails> loadedNamedColorSpaceDetails = new Dictionary<NameToken, ColorSpaceDetails>();

        private readonly Dictionary<NameToken, DictionaryToken> markedContentProperties = new Dictionary<NameToken, DictionaryToken>();

        private readonly Dictionary<NameToken, Shading> shadingsProperties = new Dictionary<NameToken, Shading>();

        private readonly Dictionary<NameToken, PatternColor> patternsProperties = new Dictionary<NameToken, PatternColor>();

        private (NameToken? name, IFont? font) lastLoadedFont;

        public ResourceStore(IPdfTokenScanner scanner,
            IFontFactory fontFactory,
            ILookupFilterProvider filterProvider,
            ParsingOptions parsingOptions)
        {
            this.scanner = scanner;
            this.fontFactory = fontFactory;
            this.filterProvider = filterProvider;
            this.parsingOptions = parsingOptions;
        }

        public void LoadResourceDictionary(DictionaryToken resourceDictionary)
        {
            lastLoadedFont = (null, null);
            loadedNamedColorSpaceDetails.Clear();

            namedColorSpaces.Push();
            currentFontState.Push();
            currentXObjectState.Push();

            if (resourceDictionary.TryGet(NameToken.Font, out var fontBase))
            {
                var fontDictionary = DirectObjectFinder.Get<DictionaryToken>(fontBase, scanner);

                LoadFontDictionary(fontDictionary);
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

                    currentXObjectState[NameToken.Create(pair.Key)] = reference.Data;
                }
            }

            if (resourceDictionary.TryGet(NameToken.ExtGState, scanner, out DictionaryToken? extGStateDictionaryToken))
            {
                foreach (var pair in extGStateDictionaryToken.Data)
                {
                    var name = NameToken.Create(pair.Key);
                    var state = DirectObjectFinder.Get<DictionaryToken>(pair.Value, scanner);

                    extendedGraphicsStates[name] = state;
                }
            }

            if (resourceDictionary.TryGet(NameToken.ColorSpace, scanner, out DictionaryToken? colorSpaceDictionary))
            {
                foreach (var nameColorSpacePair in colorSpaceDictionary.Data)
                {
                    var name = NameToken.Create(nameColorSpacePair.Key);

                    if (DirectObjectFinder.TryGet(nameColorSpacePair.Value, scanner, out NameToken? colorSpaceName))
                    {
                        namedColorSpaces[name] = new ResourceColorSpace(colorSpaceName);
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

                        namedColorSpaces[name] = new ResourceColorSpace(arrayNamedColorSpace, colorSpaceArray);
                    }
                    else if (parsingOptions.UseLenientParsing &&
                             DirectObjectFinder.TryGet(nameColorSpacePair.Value, scanner, out DictionaryToken? dict) &&
                             dict.TryGet(NameToken.ColorSpace, scanner, out NameToken? csName))
                    {
                        // See issue #1061
                        namedColorSpaces[name] = new ResourceColorSpace(csName);
                    }
                    else
                    {
                        throw new PdfDocumentFormatException($"Invalid ColorSpace token encountered in page resource dictionary: {nameColorSpacePair.Value}.");
                    }
                }
            }

            if (resourceDictionary.TryGet(NameToken.Pattern, scanner, out DictionaryToken? patternDictionary))
            {
                // NB: in PDF, all patterns shall be local to the context in which they are defined.
                foreach (var namePatternPair in patternDictionary.Data)
                {
                    var name = NameToken.Create(namePatternPair.Key);
                    patternsProperties[name] = PatternParser.Create(namePatternPair.Value, scanner, this, filterProvider);
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

                    markedContentProperties[key] = namedProperties;
                }
            }

            if (resourceDictionary.TryGet(NameToken.Shading, scanner, out DictionaryToken? shadingList))
            {
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
        }

        public void UnloadResourceDictionary()
        {
            lastLoadedFont = (null, null);
            loadedNamedColorSpaceDetails.Clear();
            currentFontState.Pop();
            currentXObjectState.Pop();
            namedColorSpaces.Pop();
        }

        private void LoadFontDictionary(DictionaryToken fontDictionary)
        {
            lastLoadedFont = (null, null);

            foreach (var pair in fontDictionary.Data)
            {
                if (pair.Value is IndirectReferenceToken objectKey)
                {
                    var reference = objectKey.Data;

                    currentFontState[NameToken.Create(pair.Key)] = reference;

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
                        loadedFonts[reference] = fontFactory.Get(fontObject);
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
                    loadedDirectFonts[NameToken.Create(pair.Key)] = fontFactory.Get(fd);
                }
                else
                {
                    continue;
                }
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
            
            // Null color space for images
            if (name is null)
            {
                return ColorSpaceDetailsParser.GetColorSpaceDetails(null, dictionary, scanner, this, filterProvider);
            }

            if (name.TryMapToColorSpace(out ColorSpace colorspaceActual))
            {
                // TODO - We need to find a way to store profile that have an actual dictionary, e.g. ICC profiles - without parsing them again
                return ColorSpaceDetailsParser.GetColorSpaceDetails(colorspaceActual, dictionary, scanner, this, filterProvider);
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
                    return ColorSpaceDetailsParser.GetColorSpaceDetails(mapped, dictionary, scanner, this, filterProvider);
                }
                else if (namedColorSpace.Data is ArrayToken array)
                {
                    var csd = ColorSpaceDetailsParser.GetColorSpaceDetails(mapped, dictionary.With(NameToken.ColorSpace, array), scanner, this, filterProvider);
                    loadedNamedColorSpaceDetails[name] = csd;
                    return csd;
                }
            }

            throw new InvalidOperationException($"Could not find color space for token '{name}'.");
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

        public DictionaryToken? GetExtendedGraphicsStateDictionary(NameToken name)
        {
            if (parsingOptions.UseLenientParsing && !extendedGraphicsStates.ContainsKey(name))
            {
                parsingOptions.Logger.Error($"The graphic state dictionary does not contain the key '{name}'.");
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
            return patternsProperties;
        }
    }
}
