namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using Cmap;
    using ContentStream;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using Type1;
    using Type1.Parser;

    internal class Type1FontHandler : IFontHandler
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly CMapCache cMapCache;
        private readonly IFilterProvider filterProvider;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly IEncodingReader encodingReader;
        private readonly Type1FontParser type1FontParser;

        public Type1FontHandler(IPdfTokenScanner pdfScanner, CMapCache cMapCache, IFilterProvider filterProvider, 
            FontDescriptorFactory fontDescriptorFactory, 
            IEncodingReader encodingReader,
            Type1FontParser type1FontParser)
        {
            this.pdfScanner = pdfScanner;
            this.cMapCache = cMapCache;
            this.filterProvider = filterProvider;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.encodingReader = encodingReader;
            this.type1FontParser = type1FontParser;
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

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, fontDescriptorFactory, dictionary, isLenientParsing);

            var font = ParseType1Font(descriptor, isLenientParsing);

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

            if (encoding == null && font?.Encoding.Count > 0)
            {
                encoding = new BuiltInEncoding(font.Encoding);
            }

            return new Type1FontSimple(name, firstCharacter, lastCharacter, widths, descriptor, encoding, toUnicodeCMap);
        }

        private Type1Font ParseType1Font(FontDescriptor descriptor, bool isLenientParsing)
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
                var stream = pdfScanner.Get(descriptor.FontFile.ObjectKey.Data).Data as StreamToken;

                if (stream == null)
                {
                    return null;
                }

                var raw = new PdfRawStream(stream);

                var bytes = raw.Decode(filterProvider);

                var font = type1FontParser.Parse(new ByteArrayInputBytes(bytes));

                return font;
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
