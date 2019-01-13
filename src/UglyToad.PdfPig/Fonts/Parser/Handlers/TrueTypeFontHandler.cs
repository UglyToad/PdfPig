namespace UglyToad.PdfPig.Fonts.Parser.Handlers
{
    using System;
    using SystemFonts;
    using Cmap;
    using Encodings;
    using Exceptions;
    using Filters;
    using IO;
    using Logging;
    using Parts;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;
    using TrueType;
    using TrueType.Parser;
    using Util;

    internal class TrueTypeFontHandler : IFontHandler
    {
        private readonly ILog log;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IFilterProvider filterProvider;
        private readonly CMapCache cMapCache;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;
        private readonly IEncodingReader encodingReader;
        private readonly ISystemFontFinder systemFontFinder;

        public TrueTypeFontHandler(ILog log, IPdfTokenScanner pdfScanner, IFilterProvider filterProvider, 
            CMapCache cMapCache,
            FontDescriptorFactory fontDescriptorFactory,
            TrueTypeFontParser trueTypeFontParser,
            IEncodingReader encodingReader,
            ISystemFontFinder systemFontFinder)
        {
            this.log = log;
            this.filterProvider = filterProvider;
            this.cMapCache = cMapCache;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
            this.encodingReader = encodingReader;
            this.systemFontFinder = systemFontFinder;
            this.pdfScanner = pdfScanner;
        }

        public IFont Generate(DictionaryToken dictionary, bool isLenientParsing)
        {
            if (!dictionary.TryGetOptionalTokenDirect(NameToken.FirstChar, pdfScanner, out NumericToken firstCharacterToken))
            {
                if (!dictionary.TryGetOptionalTokenDirect(NameToken.BaseFont, pdfScanner, out NameToken baseFont))
                {
                    throw new InvalidFontFormatException($"The provided TrueType font dictionary did not contain a /FirstChar or a /BaseFont entry: {dictionary}.");
                }

                // Can use the AFM descriptor despite not being Type 1!
                var standard14Font = Standard14.GetAdobeFontMetrics(baseFont.Data);

                if (standard14Font == null)
                {
                    throw new InvalidFontFormatException($"The provided TrueType font dictionary did not have a /FirstChar and did not match a Standard 14 font: {dictionary}.");
                }

                var fileSystemFont = systemFontFinder.GetTrueTypeFont(baseFont.Data);

                var thisEncoding = encodingReader.Read(dictionary, isLenientParsing);

                return new TrueTypeStandard14FallbackSimpleFont(baseFont, standard14Font, thisEncoding, fileSystemFont);
            }

            var firstCharacter = firstCharacterToken.Int;

            var widths = FontDictionaryAccessHelper.GetWidths(pdfScanner, dictionary, isLenientParsing);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, fontDescriptorFactory, dictionary, isLenientParsing);

            // TODO: use the parsed font fully.
            var font = ParseTrueTypeFont(descriptor);

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

            return new TrueTypeSimpleFont(name, descriptor, toUnicodeCMap, encoding, font, firstCharacter, widths);
        }

        private TrueTypeFontProgram ParseTrueTypeFont(FontDescriptor descriptor)
        {
            if (descriptor.FontFile == null)
            {
                try
                {
                    return systemFontFinder.GetTrueTypeFont(descriptor.FontName.Data);
                }
                catch (Exception ex)
                {
                    log.Error($"Failed finding system font by name: {descriptor.FontName}.", ex);
                }
                // TODO: check if this font is present on the host OS. See: FileSystemFontProvider.java
                return null;
            }

            if (descriptor.FontFile.FileType != DescriptorFontFile.FontFileType.TrueType)
            {
                throw new InvalidFontFormatException(
                    $"Expected a TrueType font in the TrueType font descriptor, instead it was {descriptor.FontFile.FileType}.");
            }

            try
            {

                var fontFileStream = DirectObjectFinder.Get<StreamToken>(descriptor.FontFile.ObjectKey, pdfScanner);
            
                var fontFile = fontFileStream.Decode(filterProvider);

                var font = trueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile)));

                return font;
            }
            catch (Exception ex)
            {
                log.Error("Could not parse the TrueType font.", ex);

                return null;
            }
        }
    }
}
