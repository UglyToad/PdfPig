namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using System;
    using CidFonts;
    using Cmap;
    using Composite;
    using Core;
    using Filters;
    using Fonts;
    using Parts;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Type0FontHandler : IFontHandler
    {
        private readonly CidFontFactory cidFontFactory;
        private readonly IFilterProvider filterProvider;
        private readonly IPdfTokenScanner scanner;

        public Type0FontHandler(CidFontFactory cidFontFactory, IFilterProvider filterProvider,
            IPdfTokenScanner scanner)
        {
            this.cidFontFactory = cidFontFactory;
            this.filterProvider = filterProvider;
            this.scanner = scanner;
        }

        public IFont Generate(DictionaryToken dictionary, bool isLenientParsing)
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

                cidFont = ParseDescendant(descendantFontDictionary);
            }
            else
            {
                throw new InvalidFontFormatException("No descendant font dictionary was declared for this Type 0 font. This dictionary should contain the CIDFont for the Type 0 font. " + dictionary);
            }

            var (ucs2CMap, isChineseJapaneseOrKorean) = GetUcs2CMap(dictionary, isCMapPredefined, cidFont);

            CMap toUnicodeCMap = null;
            if (dictionary.ContainsKey(NameToken.ToUnicode))
            {
                var toUnicodeValue = dictionary.Data[NameToken.ToUnicode];

                if (DirectObjectFinder.TryGet<StreamToken>(toUnicodeValue, scanner, out var toUnicodeStream))
                {
                    var decodedUnicodeCMap = toUnicodeStream?.Decode(filterProvider);

                    if (decodedUnicodeCMap != null)
                    {
                        toUnicodeCMap = CMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap));
                    }
                }
                else if (DirectObjectFinder.TryGet<NameToken>(toUnicodeValue, scanner, out var toUnicodeName))
                {
                    toUnicodeCMap = CMapCache.Get(toUnicodeName.Data);
                }
                else
                {
                    throw new PdfDocumentFormatException($"Invalid type of toUnicode CMap encountered. Got: {toUnicodeValue}.");
                }
            }

            var font = new Type0Font(baseFont, cidFont, cMap, toUnicodeCMap, ucs2CMap, isChineseJapaneseOrKorean);

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

        private ICidFont ParseDescendant(DictionaryToken dictionary)
        {
            var type = dictionary.GetNameOrDefault(NameToken.Type);
            if (type?.Equals(NameToken.Font) != true)
            {
                throw new InvalidFontFormatException($"Expected \'Font\' dictionary but found \'{type}\'");
            }

            var result = cidFontFactory.Generate(dictionary);

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
                    var cmap = CMapCache.Get(encodingName.Data);

                    result = cmap ?? throw new InvalidOperationException("Missing CMap for " + encodingName.Data);

                    isCMapPredefined = true;
                }
                else if (value is StreamToken stream)
                {
                    var decoded = stream.Decode(filterProvider);

                    var cmap = CMapCache.Parse(new ByteArrayInputBytes(decoded));

                    result = cmap ?? throw new InvalidOperationException("Could not read CMap for " + dictionary);
                }
                else
                {
                    throw new InvalidOperationException("Could not read the encoding, expected a name or a stream but got a: " + value.GetType().Name);
                }
            }

            return result;
        }

        private static (CMap, bool isChineseJapaneseOrKorean) GetUcs2CMap(DictionaryToken dictionary, bool isCMapPredefined, ICidFont cidFont)
        {
            if (!isCMapPredefined)
            {
                return (null, false);
            }

            /*
             * If the font is a composite font that uses one of the predefined CMaps except Identity–H and Identity–V or whose descendant
             * CIDFont uses the Adobe-GB1, Adobe-CNS1, Adobe-Japan1, or Adobe-Korea1 character collection use a UCS2 CMap.
             */

            var encodingName = dictionary.GetNameOrDefault(NameToken.Encoding);

            if (encodingName == null)
            {
                return (null, false);
            }

            var isChineseJapaneseOrKorean = false;

            if (cidFont != null && string.Equals(cidFont.SystemInfo.Registry, "Adobe", StringComparison.OrdinalIgnoreCase))
            {
                isChineseJapaneseOrKorean = string.Equals(cidFont.SystemInfo.Ordering, "GB1", StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(cidFont.SystemInfo.Ordering, "CNS1", StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(cidFont.SystemInfo.Ordering, "Japan1", StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(cidFont.SystemInfo.Ordering, "Korea1", StringComparison.OrdinalIgnoreCase);
            }


            var isPredefinedIdentityMap = encodingName.Equals(NameToken.IdentityH) || encodingName.Equals(NameToken.IdentityV);

            if (isPredefinedIdentityMap && !isChineseJapaneseOrKorean)
            {
                return (null, false);
            }

            if (!isChineseJapaneseOrKorean)
            {
                return (null, false);
            }

            var fullCmapName = cidFont.SystemInfo.ToString();
            var nonUnicodeCMap = CMapCache.Get(fullCmapName);

            if (nonUnicodeCMap == null)
            {
                return (null, true);
            }

            var unicodeCMapName = $"{nonUnicodeCMap.Info.Registry}-{nonUnicodeCMap.Info.Ordering}-UCS2";

            return (CMapCache.Get(unicodeCMapName), true);
        }
    }
}
