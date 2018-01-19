namespace UglyToad.PdfPig.Fonts.Parser.Handlers
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
    using PdfPig.Parser;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using Util;

    internal class Type0FontHandler : IFontHandler
    {
        private readonly CidFontFactory cidFontFactory;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IPdfObjectScanner scanner;

        public Type0FontHandler(CidFontFactory cidFontFactory, CMapCache cMapCache, IFilterProvider filterProvider, IPdfObjectParser pdfObjectParser,
            IPdfObjectScanner scanner)
        {
            this.cidFontFactory = cidFontFactory;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.pdfObjectParser = pdfObjectParser;
            this.scanner = scanner;
        }

        public IFont Generate(DictionaryToken dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var baseFont = dictionary.GetNameOrDefault(NameToken.BaseFont);

            var cMap = ReadEncoding(dictionary, out var isCMapPredefined);

            ICidFont cidFont;

            if (TryGetFirstDescendant(dictionary, out var descendantObject))
            {
                DictionaryToken descendantFontDictionary;

                if (descendantObject is IndirectReferenceToken obj)
                {
                    var parsed = DirectObjectFinder.Get<DictionaryToken>(obj, scanner);

                    descendantFontDictionary = parsed;
                }
                else
                {
                    descendantFontDictionary = (DictionaryToken) descendantObject;
                }

                cidFont = ParseDescendant(descendantFontDictionary, reader, isLenientParsing);
            }
            else
            {
                throw new InvalidFontFormatException("No descendant font dictionary was declared for this Type 0 font. This dictionary should contain the CIDFont for the Type 0 font. " + dictionary);
            }

            var ucs2CMap = GetUcs2CMap(dictionary, isCMapPredefined, false);

            CMap toUnicodeCMap = null;
            if (dictionary.ContainsKey(NameToken.ToUnicode))
            {
                var toUnicodeValue = dictionary.Data[NameToken.ToUnicode];

                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeValue, scanner);

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            var font = new Type0Font(baseFont, cidFont, cMap, toUnicodeCMap);

            return font;
        }

        private static bool TryGetFirstDescendant(DictionaryToken dictionary, out IToken descendant)
        {
            descendant = null;

            if (!dictionary.TryGet(NameToken.DescendantFonts, out var value))
            {
                return false;
            }

            if (value is IndirectReferenceToken obj)
            {
                descendant = obj;
                return true;
            }

            if (value is ArrayToken array && array.Data.Count > 0)
            {
                if (array.Data[0] is IndirectReferenceToken objArr)
                {
                descendant = objArr;
                }
                else if (array.Data[0] is DictionaryToken dict)
                {
                    descendant = dict;
                }
                else
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private ICidFont ParseDescendant(DictionaryToken dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var type = dictionary.GetNameOrDefault(NameToken.Type);
            if (type?.Equals(NameToken.Font) != true)
            {
                throw new InvalidFontFormatException($"Expected \'Font\' dictionary but found \'{type}\'");
            }

            var result = cidFontFactory.Generate(dictionary, reader, isLenientParsing);

            return result;
        }

        private CMap ReadEncoding(DictionaryToken dictionary, out bool isCMapPredefined)
        {
            isCMapPredefined = false;
            CMap result = default(CMap);

            if (dictionary.TryGet(NameToken.Encoding, out var value))
            {
                if (value is NameToken encodingName)
                {
                    var cmap = cMapCache.Get(encodingName.Data);

                    result = cmap ?? throw new InvalidOperationException("Missing CMap for " + encodingName.Data);

                    isCMapPredefined = true;
                }
                else if (value is StreamToken stream)
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

        private static CMap GetUcs2CMap(DictionaryToken dictionary, bool isCMapPredefined, bool usesDescendantAdobeFont)
        {
            if (!isCMapPredefined)
            {
                return null;
            }

            /*
             * If the font is a composite font that uses one of the predefined CMaps except Identity–H and Identity–V or whose descendant
             * CIDFont uses the Adobe-GB1, Adobe-CNS1, Adobe-Japan1, or Adobe-Korea1 character collection use a UCS2 CMap.
             */

            var encodingName = dictionary.GetNameOrDefault(NameToken.Encoding);

            if (encodingName == null)
            {
                return null;
            }

            var isPredefinedIdentityMap = encodingName.Equals(NameToken.IdentityH) || encodingName.Equals(NameToken.IdentityV);

            if (isPredefinedIdentityMap && !usesDescendantAdobeFont)
            {
                return null;
            }

            throw new NotSupportedException("Support for UCS2 CMaps are not implemented yet. Please raise an issue.");
        }
    }
}
