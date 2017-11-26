namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Fonts;
    using Parser;

    public interface IResourceStore
    {
        
    }

    public class ResourceContainer : IResourceStore
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

                var font = new CompositeFont();

                loadedFonts[pair.Key] = font;
            }
        }
    }
}

