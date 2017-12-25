namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System;
    using CidFonts;
    using Cmap;
    using Composite;
    using ContentStream;
    using Cos;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using Pdf.Parser;

    internal class Type0FontHandler : IFontHandler
    {
        private readonly CidFontFactory cidFontFactory;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly IPdfObjectParser pdfObjectParser;

        public Type0FontHandler(CidFontFactory cidFontFactory, CMapCache cMapCache, IFilterProvider filterProvider, IPdfObjectParser pdfObjectParser)
        {
            this.cidFontFactory = cidFontFactory;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.pdfObjectParser = pdfObjectParser;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var baseFont = dictionary.GetName(CosName.BASE_FONT);

            var cMap = ReadEncoding(dictionary, out var isCMapPredefined);

            if (TryGetFirstDescendant(dictionary, out var descendantObject))
            {
                var parsed = pdfObjectParser.Parse(descendantObject.ToIndirectReference(), reader, isLenientParsing);

                if (parsed is PdfDictionary descendantFontDictionary)
                {
                    ParseDescendant(descendantFontDictionary, reader, isLenientParsing);
                }
                else
                {
                    throw new InvalidFontFormatException("Expected to find a Descendant Font dictionary, instead it was: " + parsed);
                }
            }
            else
            {
                throw new InvalidFontFormatException("No descendant font dictionary was declared for this Type 0 font. This dictionary should contain the CIDFont for the Type 0 font. " + dictionary);
            }

            var ucs2CMap = GetUcs2CMap(dictionary, isCMapPredefined, false);

            CMap toUnicodeCMap = null;
            if (dictionary.ContainsKey(CosName.TO_UNICODE))
            {
                var toUnicodeValue = dictionary[CosName.TO_UNICODE];

                var toUnicode = pdfObjectParser.Parse(((CosObject)toUnicodeValue).ToIndirectReference(), reader, isLenientParsing) as RawCosStream;

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            var font = new Type0Font(baseFont, new Type0CidFont(), cMap, toUnicodeCMap);

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

        private void ParseDescendant(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var type = dictionary.GetName(CosName.TYPE);
            if (!CosName.FONT.Equals(type))
            {
                throw new InvalidOperationException($"Expected \'Font\' dictionary but found \'{type.Name}\'");
            }

            cidFontFactory.Generate(dictionary, reader, isLenientParsing);
        }

        private CMap ReadEncoding(PdfDictionary dictionary, out bool isCMapPredefined)
        {
            isCMapPredefined = false;
            CMap result = default(CMap);

            if (dictionary.TryGetValue(CosName.ENCODING, out var value))
            {
                if (value is CosName encodingName)
                {
                    var cmap = cMapCache.Get(encodingName.Name);

                    result = cmap ?? throw new InvalidOperationException("Missing CMap for " + encodingName.Name);

                    isCMapPredefined = true;
                }
                else if (value is RawCosStream stream)
                {
                    var decoded = stream.Decode(filterProvider);

                    var cmap = cMapCache.Parse(new ByteArrayInputBytes(decoded), false);

                    result = cmap ?? throw new InvalidOperationException("Could not read CMap for " + dictionary);
                }
                else
                {
                    throw new InvalidOperationException("Could not read the encoding, expected a name or a stream but got a: " + value.GetType().Name);
                }
            }

            return result;
        }

        private static CMap GetUcs2CMap(PdfDictionary dictionary, bool isCMapPredefined, bool usesDescendantAdobeFont)
        {
            if (!isCMapPredefined)
            {
                return null;
            }

            /*
             * If the font is a composite font that uses one of the predefined CMaps except Identity–H and Identity–V or whose descendant
             * CIDFont uses the Adobe-GB1, Adobe-CNS1, Adobe-Japan1, or Adobe-Korea1 character collection use a UCS2 CMap.
             */

            var encodingName = dictionary.GetName(CosName.ENCODING);

            if (encodingName == null)
            {
                return null;
            }

            var isPredefinedIdentityMap = encodingName.Equals(CosName.IDENTITY_H) || encodingName.Equals(CosName.IDENTITY_V);

            if (isPredefinedIdentityMap && !usesDescendantAdobeFont)
            {
                return null;
            }

            throw new NotSupportedException("Support for UCS2 CMaps are not implemented yet. Please raise an issue.");
        }
    }
}
