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

        internal void LoadResourceDictionary(PdfDictionary dictionary, ParsingArguments arguments)
        {
            if (dictionary.TryGetValue(CosName.FONT, out var fontBase) && fontBase is PdfDictionary fontDictionary)
            {
                LoadFontDictionary(fontDictionary, arguments);
            }
        }

        private void LoadFontDictionary(PdfDictionary fontDictionary, ParsingArguments arguments)
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

                var dynamicParser = arguments.Get<DynamicParser>();

                var fontObject = dynamicParser.Parse(arguments, objectKey, false) as PdfDictionary;

                if (fontObject == null)
                {
                    throw new InvalidOperationException($"Could not retrieve the font with name: {pair.Key} which should have been object {objectKey.GetObjectNumber()}");
                }

                loadedFonts[pair.Key] = arguments.Get<FontFactory>().GetFont(fontObject, arguments);
            }
        }
        
        public IFont GetFont(CosName name)
        {
            loadedFonts.TryGetValue(name, out var font);

            return font;
        }
    }
}

