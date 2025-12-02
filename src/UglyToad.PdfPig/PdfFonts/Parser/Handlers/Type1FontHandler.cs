namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using Cmap;
    using Core;
    using Filters;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.Encodings;
    using Fonts.Standard14Fonts;
    using Fonts.Type1;
    using Fonts.Type1.Parser;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;

    internal class Type1FontHandler : IFontHandler
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly ILookupFilterProvider filterProvider;
        private readonly IEncodingReader encodingReader;
        private readonly CMapLocalCache cmapLocalCache;
        private readonly bool isLenientParsing;
        private readonly StackDepthGuard stackDepthGuard;

        public Type1FontHandler(
            IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            IEncodingReader encodingReader,
            CMapLocalCache cmapLocalCache,
            StackDepthGuard stackDepthGuard,
            bool isLenientParsing)
        {
            this.pdfScanner = pdfScanner;
            this.filterProvider = filterProvider;
            this.encodingReader = encodingReader;
            this.cmapLocalCache = cmapLocalCache;
            this.isLenientParsing = isLenientParsing;
            this.stackDepthGuard = stackDepthGuard;
        }

        public IFont Generate(DictionaryToken dictionary)
        {
            var usingStandard14Only = !dictionary.ContainsKey(NameToken.FirstChar) || !dictionary.ContainsKey(NameToken.Widths);

            if (usingStandard14Only)
            {
                // TODO: some fonts combine standard 14 font with other metrics.
                if (!dictionary.TryGet(NameToken.BaseFont, out var baseFontToken)
                    || !(baseFontToken is NameToken standard14Name))
                {
                    throw new InvalidFontFormatException($"The Type 1 font did not contain a first character entry but also did not reference a standard 14 font: {dictionary}");
                }

                var metrics = Standard14.GetAdobeFontMetrics(standard14Name.Data);

                if (metrics != null)
                {
                    var overrideEncoding = encodingReader.Read(dictionary);

                    return new Type1Standard14Font(metrics, overrideEncoding);
                }
            }

            int firstCharacter, lastCharacter;
            double[] widths;
            if (!usingStandard14Only)
            {
                firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

                lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

                widths = FontDictionaryAccessHelper.GetWidths(pdfScanner, dictionary);
            }
            else
            {
                firstCharacter = 0;
                lastCharacter = 0;
                widths = [];
            }

            if (!dictionary.TryGet(NameToken.FontDescriptor, out _))
            {
                if (dictionary.TryGet(NameToken.BaseFont, pdfScanner, out NameToken? baseFontToken))
                {
                    var metrics = Standard14.GetAdobeFontMetrics(baseFontToken.Data);

                    if (metrics is null)
                    {
                        if (isLenientParsing)
                        {
                            // We can support a fallback here to return content.
                            // https://github.com/apache/pdfbox/blob/f81c7c5a06126db68aa985a0e755cdbffed7d270/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/FontMapperImpl.java#L304
                            metrics = Standard14.GetAdobeFontMetrics(Standard14Font.TimesRoman);
                        }
                        else
                        {
                            throw new PdfDocumentFormatException(
                                $"Type 1 Standard 14 font with name {baseFontToken} requested, this is an invalid name.");
                        }
                    }

                    var overrideEncoding = encodingReader.Read(dictionary);

                    return new Type1Standard14Font(metrics, overrideEncoding);
                }
            }

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, dictionary);

            var font = ParseFontProgram(descriptor);

            var name = FontDictionaryAccessHelper.GetName(pdfScanner, dictionary, descriptor);

            CMap? toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, pdfScanner);

                cmapLocalCache.TryGet(toUnicode, out toUnicodeCMap);
            }

            var fromFont = default(Encoding);
            if (font != null)
            {
                if (font.TryGetFirst(out var t1Font))
                {
                    fromFont = t1Font.Encoding is not null ? new BuiltInEncoding(t1Font.Encoding) : default(Encoding);
                }
                else if (font.TryGetSecond(out var cffFont))
                {
                    fromFont = cffFont.FirstFont?.Encoding;
                }
            }

            var encoding = encodingReader.Read(dictionary, descriptor, fromFont);

            if (encoding is null && font != null && font.TryGetFirst(out var t1FontReplacement))
            {
                encoding = new BuiltInEncoding(t1FontReplacement.Encoding);
            }

            return new Type1FontSimple(name, firstCharacter, lastCharacter, widths, descriptor, encoding!, toUnicodeCMap!, font!);
        }

        private Union<Type1Font, CompactFontFormatFontCollection>? ParseFontProgram(FontDescriptor descriptor)
        {
            if (descriptor?.FontFile is null)
            {
                return null;
            }

            if (descriptor.FontFile.ObjectKey.Data.ObjectNumber == 0)
            {
                return null;
            }

            try
            {
                if (!(pdfScanner.Get(descriptor.FontFile.ObjectKey.Data)?.Data is StreamToken stream))
                {
                    return null;
                }

                var bytes = stream.Decode(filterProvider, pdfScanner);

                // We have a Compact Font Format font rather than an Adobe Type 1 Font.
                if (stream.StreamDictionary.TryGet(NameToken.Subtype, out NameToken subTypeName)
                && NameToken.Type1C.Equals(subTypeName))
                {
                    var cffFont = CompactFontFormatParser.Parse(new CompactFontFormatData(bytes));
                    return Union<Type1Font, CompactFontFormatFontCollection>.Two(cffFont);
                }

                var length1 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length1, pdfScanner);
                var length2 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length2, pdfScanner);

                var font = Type1FontParser.Parse(new MemoryInputBytes(bytes), length1.Int, length2.Int, stackDepthGuard);

                return Union<Type1Font, CompactFontFormatFontCollection>.One(font);
            }
            catch
            {
                // ignored.
            }

            return null;
        }
    }
}
