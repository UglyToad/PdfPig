namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Fonts;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal class ResourceContainer : IResourceStore
    {
        private readonly IPdfTokenScanner scanner;
        private readonly IFontFactory fontFactory;

        private readonly Dictionary<IndirectReference, IFont> loadedFonts = new Dictionary<IndirectReference, IFont>();
        private readonly Dictionary<NameToken, IndirectReference> currentResourceState = new Dictionary<NameToken, IndirectReference>();

        private readonly Dictionary<NameToken, DictionaryToken> extendedGraphicsStates = new Dictionary<NameToken, DictionaryToken>();

        public ResourceContainer(IPdfTokenScanner scanner, IFontFactory fontFactory)
        {
            this.scanner = scanner;
            this.fontFactory = fontFactory;
        }

        public void LoadResourceDictionary(DictionaryToken resourceDictionary, bool isLenientParsing)
        {
            if (resourceDictionary.TryGet(NameToken.Font, out var fontBase))
            {
                var fontDictionary = DirectObjectFinder.Get<DictionaryToken>(fontBase, scanner);

                LoadFontDictionary(fontDictionary, isLenientParsing);
            }

            if (resourceDictionary.TryGet(NameToken.Xobject, out var xobjectBase))
            {
                var xobjectDictionary = DirectObjectFinder.Get<DictionaryToken>(xobjectBase, scanner);

                foreach (var pair in xobjectDictionary.Data)
                {
                    if (!(pair.Value is IndirectReferenceToken reference))
                    {
                        throw new InvalidOperationException($"Expected the XObject dictionary value for key /{pair.Key} to be an indirect reference, instead got: {pair.Value}.");
                    }

                    currentResourceState[NameToken.Create(pair.Key)] = reference.Data;
                }
            }

            if (resourceDictionary.TryGet(NameToken.ExtGState, scanner, out DictionaryToken extGStateDictionaryToken))
            {
                foreach (var pair in extGStateDictionaryToken.Data)
                {
                    var name = NameToken.Create(pair.Key);
                    var state = DirectObjectFinder.Get<DictionaryToken>(pair.Value, scanner);

                    extendedGraphicsStates[name] = state;
                }
            }
        }

        private void LoadFontDictionary(DictionaryToken fontDictionary, bool isLenientParsing)
        {
            foreach (var pair in fontDictionary.Data)
            {
                if (!(pair.Value is IndirectReferenceToken objectKey))
                {
                    if (isLenientParsing)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"The font with name {pair.Key} did not link to an object key. Value was: {pair.Value}.");
                }

                var reference = objectKey.Data;

                currentResourceState[NameToken.Create(pair.Key)] = reference;

                if (loadedFonts.ContainsKey(reference))
                {
                    continue;
                }

                var fontObject = DirectObjectFinder.Get<DictionaryToken>(objectKey, scanner);

                if (fontObject == null)
                {
                    throw new InvalidOperationException($"Could not retrieve the font with name: {pair.Key} which should have been object {objectKey}");
                }

                loadedFonts[reference] = fontFactory.Get(fontObject, isLenientParsing);
            }
        }

        public IFont GetFont(NameToken name)
        {
            var reference = currentResourceState[name];

            loadedFonts.TryGetValue(reference, out var font);

            return font;
        }

        public IFont GetFontDirectly(IndirectReferenceToken fontReferenceToken, bool isLenientParsing)
        {
            if (!DirectObjectFinder.TryGet(fontReferenceToken, scanner, out DictionaryToken fontDictionaryToken))
            {
                throw new PdfDocumentFormatException($"The requested font reference token {fontReferenceToken} wasn't a font.");
            }

            var font = fontFactory.Get(fontDictionaryToken, isLenientParsing);

            return font;
        }

        public StreamToken GetXObject(NameToken name)
        {
            var reference = currentResourceState[name];

            var stream = DirectObjectFinder.Get<StreamToken>(new IndirectReferenceToken(reference), scanner);

            return stream;
        }

        public DictionaryToken GetExtendedGraphicsStateDictionary(NameToken name)
        {
            return extendedGraphicsStates[name];
        }
    }
}
