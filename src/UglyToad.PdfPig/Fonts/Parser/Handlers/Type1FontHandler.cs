namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using System.Linq;
    using Cmap;
    using CompactFontFormat;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;
    using Type1;
    using Type1.Parser;
    using Util;

    internal class Type1FontHandler : IFontHandler
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly IEncodingReader encodingReader;
        private readonly Type1FontParser type1FontParser;
        private readonly CompactFontFormatParser compactFontFormatParser;

        public Type1FontHandler(IPdfTokenScanner pdfScanner, CMapCache cMapCache, IFilterProvider filterProvider, 
            FontDescriptorFactory fontDescriptorFactory, 
            IEncodingReader encodingReader,
            Type1FontParser type1FontParser,
            CompactFontFormatParser compactFontFormatParser)
        {
            this.pdfScanner = pdfScanner;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.encodingReader = encodingReader;
            this.type1FontParser = type1FontParser;
            this.compactFontFormatParser = compactFontFormatParser;
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

                return new Type1Standard14Font(metrics);
            }

            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

            var widths = FontDictionaryAccessHelper.GetWidths(pdfScanner, dictionary, isLenientParsing);

            if (!dictionary.TryGet(NameToken.FontDescriptor, out var _))
            {
                if (dictionary.TryGet(NameToken.BaseFont, out var baseFontToken)  && 
                    DirectObjectFinder.TryGet(baseFontToken, pdfScanner, out NameToken baseFontName))
                {
                    var metrics = Standard14.GetAdobeFontMetrics(baseFontName.Data);

                    var overrideEncoding = encodingReader.Read(dictionary, isLenientParsing);

                    return new Type1Standard14Font(metrics, overrideEncoding);
                }
            }

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, fontDescriptorFactory, dictionary, isLenientParsing);

            var font = ParseFontProgram(descriptor, isLenientParsing);

            var name = FontDictionaryAccessHelper.GetName(pdfScanner, dictionary, descriptor, isLenientParsing);
            
            CMap toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, pdfScanner);

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            Encoding encoding = encodingReader.Read(dictionary, isLenientParsing, descriptor);

            if (encoding == null)
            {
                font?.Match(x => encoding = new BuiltInEncoding(x.Encoding), _ => {});
            }

            return new Type1FontSimple(name, firstCharacter, lastCharacter, widths, descriptor, encoding, toUnicodeCMap, font);
        }

        private Union<Type1FontProgram, CompactFontFormatFontProgram> ParseFontProgram(FontDescriptor descriptor, bool isLenientParsing)
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
                    var cffFont = compactFontFormatParser.Parse(new CompactFontFormatData(bytes));
                    return Union<Type1FontProgram, CompactFontFormatFontProgram>.Two(cffFont);
                }
                
                var length1 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length1, pdfScanner);
                var length2 = stream.StreamDictionary.Get<NumericToken>(NameToken.Length2, pdfScanner);
                
                var font = type1FontParser.Parse(new ByteArrayInputBytes(bytes), length1.Int, length2.Int);

                return Union<Type1FontProgram, CompactFontFormatFontProgram>.One(font);
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
