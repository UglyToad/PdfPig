namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using Cmap;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using TrueType;
    using TrueType.Parser;

    internal class TrueTypeFontHandler : IFontHandler
    {
        private readonly IFilterProvider filterProvider;
        private readonly CMapCache cMapCache;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;
        private readonly IEncodingReader encodingReader;
        private readonly IPdfTokenScanner pdfScanner;

        public TrueTypeFontHandler(IPdfTokenScanner pdfScanner, IFilterProvider filterProvider, 
            CMapCache cMapCache,
            FontDescriptorFactory fontDescriptorFactory,
            TrueTypeFontParser trueTypeFontParser,
            IEncodingReader encodingReader)
        {
            this.filterProvider = filterProvider;
            this.cMapCache = cMapCache;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
            this.encodingReader = encodingReader;
            this.pdfScanner = pdfScanner;
        }

        public IFont Generate(DictionaryToken dictionary, bool isLenientParsing)
        {
            var firstCharacter = FontDictionaryAccessHelper.GetFirstCharacter(dictionary);

            var lastCharacter = FontDictionaryAccessHelper.GetLastCharacter(dictionary);

            var widths = FontDictionaryAccessHelper.GetWidths(pdfScanner, dictionary, isLenientParsing);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, fontDescriptorFactory, dictionary, isLenientParsing);

            // TODO: use the parsed font fully.
            //var font = ParseTrueTypeFont(descriptor, reader, isLenientParsing);

            var name = FontDictionaryAccessHelper.GetName(pdfScanner, dictionary, descriptor, isLenientParsing);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, pdfScanner);

                var decodedUnicodeCMap = toUnicode.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            Encoding encoding = encodingReader.Read(dictionary, isLenientParsing, descriptor);

            return new TrueTypeSimpleFont(name, firstCharacter, lastCharacter, widths, descriptor, toUnicodeCMap, encoding);
        }

        private TrueTypeFont ParseTrueTypeFont(FontDescriptor descriptor, IRandomAccessRead reader,
            bool isLenientParsing)
        {
            if (descriptor?.FontFile == null)
            {
                return null;
            }

            if (descriptor.FontFile.FileType != DescriptorFontFile.FontFileType.TrueType)
            {
                throw new InvalidFontFormatException(
                    $"Expected a TrueType font in the TrueType font descriptor, instead it was {descriptor.FontFile.FileType}.");
            }

            //var fontFileStream = pdfObjectParser.Parse(descriptor.FontFile.ObjectKey, reader, isLenientParsing) as PdfRawStream;

            //if (fontFileStream == null)
            {
                return null;
            }

            //var fontFile = fontFileStream.Decode(filterProvider);

            //var font = trueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile)));

            //return font;
        }
    }
}
