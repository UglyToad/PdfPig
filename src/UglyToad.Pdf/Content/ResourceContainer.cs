namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Fonts;
    using IO;
    using Parser;

    internal class ResourceContainer : IResourceStore
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IFontFactory fontFactory;

        private readonly Dictionary<CosName, IFont> loadedFonts = new Dictionary<CosName, IFont>();

        public ResourceContainer(IPdfObjectParser pdfObjectParser, IFontFactory fontFactory)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.fontFactory = fontFactory;
        }

        public void LoadResourceDictionary(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (dictionary.TryGetValue(CosName.FONT, out var fontBase))
            {
                PdfDictionary fontDictionary;
                if (fontBase is CosObject obj)
                {
                    var parsedObj = pdfObjectParser.Parse(obj.ToIndirectReference(), reader, isLenientParsing);

                    if (parsedObj is PdfDictionary indirectFontDictionary)
                    {
                        fontDictionary = indirectFontDictionary;
                    }
                    else
                    {
                        throw new InvalidOperationException($"No font dictionary could be found for the dictionary {dictionary}.");
                    }
                }
                else if (fontBase is PdfDictionary directDictionary)
                {
                    fontDictionary = directDictionary;
                }
                else
                {
                    throw new InvalidOperationException($"No font dictionary could be found for the dictionary {dictionary}");
                }

                LoadFontDictionary(fontDictionary, reader, isLenientParsing);
            }
        }

        private void LoadFontDictionary(PdfDictionary fontDictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            foreach (var pair in fontDictionary)
            {
                if (loadedFonts.ContainsKey(pair.Key))
                {
                    continue;
                }

                if (!(pair.Value is CosObject objectKey))
                {
                    if (isLenientParsing)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"The font with name {pair.Key} did not link to an object key. Value was: {pair.Value}.");
                }
                
                var fontObject = pdfObjectParser.Parse(objectKey.ToIndirectReference(), reader, false) as PdfDictionary;

                if (fontObject == null)
                {
                    throw new InvalidOperationException($"Could not retrieve the font with name: {pair.Key} which should have been object {objectKey.GetObjectNumber()}");
                }

                loadedFonts[pair.Key] = fontFactory.Get(fontObject, reader, isLenientParsing);
            }
        }
        
        public IFont GetFont(CosName name)
        {
            loadedFonts.TryGetValue(name, out var font);

            return font;
        }
    }
}

