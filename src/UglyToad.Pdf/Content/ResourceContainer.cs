namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Filters;
    using Fonts;
    using Fonts.Cmap;
    using Fonts.Parser;
    using IO;
    using Parser;

    internal interface IResourceStore
    {
        IFont GetFont(CosName name);
    }

    internal class ResourceContainer : IResourceStore
    {
        private readonly Dictionary<CosName, IFont> loadedFonts = new Dictionary<CosName, IFont>();

        internal void LoadResourceDictionary(ContentStreamDictionary dictionary, ParsingArguments arguments)
        {
            if (dictionary.TryGetValue(CosName.FONT, out var fontBase) && fontBase is ContentStreamDictionary fontDictionary)
            {
                LoadFontDictionary(fontDictionary, arguments);
            }
        }

        private void LoadFontDictionary(ContentStreamDictionary fontDictionary, ParsingArguments arguments)
        {
            foreach (var pair in fontDictionary)
            {
                if (loadedFonts.ContainsKey(pair.Key))
                {
                    continue;
                }

                if (!(pair.Value is CosObject objectKey))
                {
                    if (arguments.IsLenientParsing)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"The font with name {pair.Key} did not link to an object key. Value was: {pair.Value}.");
                }

                var dynamicParser = arguments.Container.Get<DynamicParser>();

                var fontObject = dynamicParser.Parse(arguments, objectKey, false) as ContentStreamDictionary;

                if (fontObject == null)
                {
                    throw new InvalidOperationException($"Could not retrieve the font with name: {pair.Key} which should have been object {objectKey.GetObjectNumber()}");
                }

                CMap toUnicodeCMap = null;
                if (fontObject.ContainsKey(CosName.TO_UNICODE))
                {
                    var toUnicodeValue = fontObject[CosName.TO_UNICODE];

                    var toUnicode = dynamicParser.Parse(arguments, toUnicodeValue as CosObject, false) as RawCosStream;

                    var decodedUnicodeCMap = toUnicode?.Decode(arguments.Container.Get<IFilterProvider>());

                    if (decodedUnicodeCMap != null)
                    {
                        toUnicodeCMap = arguments.Container.Get<CMapParser>()
                            .Parse(new ByteArrayInputBytes(decodedUnicodeCMap), arguments.IsLenientParsing);
                    }

                    
                }

                var font = new CompositeFont
                {
                    Name = pair.Key,
                    SubType = fontObject.GetName(CosName.SUBTYPE),
                    ToUnicode = toUnicodeCMap
                };

                loadedFonts[pair.Key] = font;
            }
        }

        public IFont GetFont(CosName name)
        {
            loadedFonts.TryGetValue(name, out var font);

            return font;
        }
    }
}

