namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using Cmap;
    using Core;
    using Filters;
    using Fonts;
    using Fonts.AdobeFontMetrics;
    using Fonts.CompactFontFormat;
    using Fonts.Encodings;
    using Fonts.Standard14Fonts;
    using Fonts.Type1;
    using Fonts.SystemFonts;
    using Fonts.Type1.Parser;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;

    internal sealed class Type1FontHandler : IFontHandler
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

        private Encoding? ReadStandard14Encoding(DictionaryToken dictionary, AdobeFontMetrics metrics)
        {
            return encodingReader.Read(dictionary, null, GetSymbolicBuiltInEncoding(metrics));
        }

        /// <summary>
        /// A /Differences array without /BaseEncoding applies on top of the font's built-in encoding. For the
        /// symbolic Standard 14 fonts (Symbol, ZapfDingbats) that is their own encoding, not StandardEncoding.
        /// </summary>
        /// <returns>The built-in encoding of a symbolic Standard 14 font, <see langword="null"/> for the others.</returns>
        internal static Encoding? GetSymbolicBuiltInEncoding(AdobeFontMetrics metrics)
        {
            return string.Equals(metrics.EncodingScheme, "FontSpecific", StringComparison.Ordinal)
                ? new AdobeFontMetricsEncoding(metrics)
                : null;
        }

        /// <summary>
        /// The Standard 14 metrics for a font drawn without its own program: by exact name, else by the loose match of
        /// <see cref="Standard14.TryGetStandard14Font"/> (subset tag, case, spaces, hyphens and commas ignored).
        /// </summary>
        private static AdobeFontMetrics? GetStandard14MetricsLoosely(string fontName)
        {
            return Standard14.GetAdobeFontMetrics(fontName)
                   ?? (Standard14.TryGetStandard14Font(fontName, out var font) ? Standard14.GetAdobeFontMetrics(font) : null);
        }

        private static SubstituteFont? GetSubstitute(string? fontName)
        {
            SystemFontFinder.TryGetSubstituteFont(fontName, out var substitute);
            return substitute;
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
                    var overrideEncoding = ReadStandard14Encoding(dictionary, metrics);

                    return new Type1Standard14Font(metrics, overrideEncoding, GetSubstitute(metrics.FontName));
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
                            // We can support a fallback here to return content: a loose match of the name (e.g.
                            // ABCDEF+Helvetica), else Times-Roman as PDFBox does without a descriptor.
                            // https://github.com/apache/pdfbox/blob/f81c7c5a06126db68aa985a0e755cdbffed7d270/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/FontMapperImpl.java#L304
                            metrics = GetStandard14MetricsLoosely(baseFontToken.Data)
                                      ?? Standard14.GetAdobeFontMetrics(Standard14Font.TimesRoman);
                        }
                        else
                        {
                            throw new PdfDocumentFormatException(
                                $"Type 1 Standard 14 font with name {baseFontToken} requested, this is an invalid name.");
                        }
                    }

                    var overrideEncoding = ReadStandard14Encoding(dictionary, metrics);

                    return new Type1Standard14Font(metrics, overrideEncoding, GetSubstitute(metrics.FontName));
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
                    fromFont = (t1Font.Encoding is not null && t1Font.Encoding.Count > 0) ? new BuiltInEncoding(t1Font.Encoding) : default(Encoding);
                }
                else if (font.TryGetSecond(out var cffFont))
                {
                    fromFont = cffFont.FirstFont?.Encoding;
                }
            }

            AdobeFontMetrics? standard14Metrics = null;
            if (font is null && dictionary.TryGet(NameToken.BaseFont, pdfScanner, out NameToken? baseFont))
            {
                standard14Metrics = GetStandard14MetricsLoosely(baseFont.Data);
                if (standard14Metrics is not null)
                {
                    // No program to read the built-in encoding from: it is the Standard 14 font's.
                    fromFont = GetSymbolicBuiltInEncoding(standard14Metrics);
                }
            }

            var encoding = encodingReader.Read(dictionary, descriptor, fromFont);

            if (encoding is null && font != null && font.TryGetFirst(out var t1FontReplacement) && t1FontReplacement.Encoding?.Count > 0)
            {
                encoding = new BuiltInEncoding(t1FontReplacement.Encoding);
            }

            return new Type1FontSimple(name, firstCharacter, lastCharacter, widths, descriptor, encoding!, toUnicodeCMap!, font!,
                standard14Metrics, standard14Metrics is null ? null : GetSubstitute(standard14Metrics.FontName));
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

                // We have a Compact Font Format font rather than an Adobe Type 1 Font: bare (Type1C) or as the CFF table
                // of an OpenType font with PostScript outlines (OpenType).
                if (stream.StreamDictionary.TryGet(NameToken.Subtype, out NameToken subTypeName)
                    && (NameToken.Type1C.Equals(subTypeName) || NameToken.OpenType.Equals(subTypeName)))
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
