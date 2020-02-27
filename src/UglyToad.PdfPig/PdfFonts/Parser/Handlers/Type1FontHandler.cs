namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using System.Linq;
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
        private readonly IFilterProvider filterProvider;
        private readonly IEncodingReader encodingReader;

        public Type1FontHandler(IPdfTokenScanner pdfScanner, IFilterProvider filterProvider,
            IEncodingReader encodingReader)
        {
            this.pdfScanner = pdfScanner;
            this.filterProvider = filterProvider;
            this.encodingReader = encodingReader;
        }

        public IFont Generate(DictionaryToken dictionary, bool isLenientParsing)
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
                    var overrideEncoding = encodingReader.Read(dictionary, isLenientParsing);

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
                widths = EmptyArray<double>.Instance;
            }

            if (!dictionary.TryGet(NameToken.FontDescriptor, out var _))
            {
                if (dictionary.TryGet(NameToken.BaseFont, out var baseFontToken) &&
                    DirectObjectFinder.TryGet(baseFontToken, pdfScanner, out NameToken baseFontName))
                {
                    var metrics = Standard14.GetAdobeFontMetrics(baseFontName.Data);

                    var overrideEncoding = encodingReader.Read(dictionary, isLenientParsing);

                    return new Type1Standard14Font(metrics, overrideEncoding);
                }
            }

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, dictionary);

            var font = ParseFontProgram(descriptor, isLenientParsing);

            var name = FontDictionaryAccessHelper.GetName(pdfScanner, dictionary, descriptor, isLenientParsing);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, pdfScanner);

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = CMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap));
                }
            }

            Encoding fromFont = font?.Match(x => x.Encoding != null ? new BuiltInEncoding(x.Encoding) : default(Encoding), x =>
            {
                if (x.Fonts != null && x.Fonts.Count > 0)
                {
                    return x.Fonts.First().Value.Encoding;
                }

                return default(Encoding);
            });

            Encoding encoding = encodingReader.Read(dictionary, isLenientParsing, descriptor, fromFont);

            if (encoding == null)
            {
                font?.Match(x => encoding = new BuiltInEncoding(x.Encoding), _ => { });
            }

            return new Type1FontSimple(name, firstCharacter, lastCharacter, widths, descriptor, encoding, toUnicodeCMap, font);
        }

        private Union<Type1Font, CompactFontFormatFontCollection> ParseFontProgram(FontDescriptor descriptor, bool isLenientParsing)
        {
            if (descriptor?.FontFile == null)
            {
                return null;
            }

            if (descriptor.FontFile.ObjectKey.Data.ObjectNumber == 0)
            {
                return null;
            }

            try
            {
                if (!(pdfScanner.Get(descriptor.FontFile.ObjectKey.Data).Data is StreamToken stream))
                {
                    return null;
                }

                var bytes = stream.Decode(filterProvider);

                // We have a Compact Font Format font rather than an Adobe Type 1 Font.
                if (stream.StreamDictionary.TryGet(NameToken.Subtype, out NameToken subTypeName)
                && NameToken.Type1C.Equals(subTypeName))
                {
                    var cffFont = CompactFontFormatParser.Parse(new CompactFontFormatData(bytes));
                    return Union<Type1Font, CompactFontFormatFontCollection>.Two(cffFont);
                }

                var length1 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length1, pdfScanner);
                var length2 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length2, pdfScanner);

                var font = Type1FontParser.Parse(new ByteArrayInputBytes(bytes), length1.Int, length2.Int);

                return Union<Type1Font, CompactFontFormatFontCollection>.One(font);
            }
            catch
            {
                if (!isLenientParsing)
                {
                    throw;
                }
            }

            return null;
        }
    }
}
