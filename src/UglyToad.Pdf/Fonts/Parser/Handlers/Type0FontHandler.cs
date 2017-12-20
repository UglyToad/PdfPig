namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System;
    using Cmap;
    using ContentStream;
    using Cos;
    using Filters;
    using IO;
    using Parts;
    using Pdf.Parser;

    internal class Type0FontHandler : IFontHandler
    {
        private readonly CidFontFactory cidFontFactory;
        private readonly CMapCache cMapCache;

        public Type0FontHandler(CidFontFactory cidFontFactory, CMapCache cMapCache)
        {
            this.cidFontFactory = cidFontFactory;
            this.cMapCache = cMapCache;
        }

        public IFont Generate(PdfDictionary dictionary, ParsingArguments arguments)
        {
            var dynamicParser = arguments.Get<DynamicParser>();

            var baseFont = dictionary.GetName(CosName.BASE_FONT);

            ReadEncoding(dictionary);

            if (TryGetFirstDescendant(dictionary, out var descendantObject))
            {
                var parsed = dynamicParser.Parse(arguments, descendantObject, false);

                if (parsed is PdfDictionary descendantFontDictionary)
                {
                    ParseDescendant(descendantFontDictionary, arguments);
                }
            }

            CMap toUnicodeCMap = null;
            if (dictionary.ContainsKey(CosName.TO_UNICODE))
            {
                var toUnicodeValue = dictionary[CosName.TO_UNICODE];

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
                SubType = CosName.TYPE0,
                ToUnicode = toUnicodeCMap,
                BaseFont = baseFont
            };

            return font;
        }
        
        private static bool TryGetFirstDescendant(PdfDictionary dictionary, out CosObject descendant)
        {
            descendant = null;

            if (!dictionary.TryGetValue(CosName.DESCENDANT_FONTS, out var value))
            {
                return false;
            }

            if (value is CosObject obj)
            {
                descendant = obj;
                return true;
            }

            if (value is COSArray array && array.Count > 0 && array.get(0) is CosObject objArr)
            {
                descendant = objArr;
                return true;
            }

            return false;
        }

        private void ParseDescendant(PdfDictionary dictionary, ParsingArguments arguments)
        {
            var type = dictionary.GetName(CosName.TYPE);
            if (!CosName.FONT.Equals(type))
            {
                throw new InvalidOperationException($"Expected \'Font\' dictionary but found \'{type.Name}\'");
            }

            cidFontFactory.Generate(dictionary, arguments, arguments.IsLenientParsing);
        }

        private void ReadEncoding(PdfDictionary dictionary)
        {
            if (dictionary.TryGetValue(CosName.ENCODING, out var value))
            {
                if (value is CosName encodingName)
                {
                    var cmap = cMapCache.Get(encodingName.Name);

                    if (cmap == null)
                    {
                        throw new InvalidOperationException("Missing CMap for " + encodingName.Name);
                    }
                }
                else if (value is RawCosStream stream)
                {
                    
                }
                else
                {
                    throw new InvalidOperationException("Could not read the encoding, expected a name or a stream but got a: " + value.GetType().Name);
                }
            }
        }
    }
}
