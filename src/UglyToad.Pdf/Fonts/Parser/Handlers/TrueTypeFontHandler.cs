namespace UglyToad.Pdf.Fonts.Parser.Handlers
{
    using System.Linq;
    using Cmap;
    using ContentStream;
    using Cos;
    using Exceptions;
    using Filters;
    using IO;
    using Parts;
    using Pdf.Parser;
    using Simple;
    using TrueType;
    using TrueType.Parser;

    internal class TrueTypeFontHandler : IFontHandler
    {
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IFilterProvider filterProvider;
        private readonly CMapCache cMapCache;
        private readonly FontDescriptorFactory fontDescriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;

        public TrueTypeFontHandler(IPdfObjectParser pdfObjectParser, IFilterProvider filterProvider, 
            CMapCache cMapCache,
            FontDescriptorFactory fontDescriptorFactory,
            TrueTypeFontParser trueTypeFontParser)
        {
            this.pdfObjectParser = pdfObjectParser;
            this.filterProvider = filterProvider;
            this.cMapCache = cMapCache;
            this.fontDescriptorFactory = fontDescriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
        }

        public IFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var firstCharacter = GetFirstCharacter(dictionary);

            var lastCharacter = GetLastCharacter(dictionary);

            var widths = GetWidths(dictionary);

            var descriptor = GetFontDescriptor(dictionary, reader, isLenientParsing);

            var font = ParseTrueTypeFont(descriptor, reader, isLenientParsing);

            var name = GetName(dictionary, descriptor);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGetItemOfType(CosName.TO_UNICODE, out CosObject toUnicodeObj))
            {
                var toUnicode = pdfObjectParser.Parse(toUnicodeObj.ToIndirectReference(), reader, isLenientParsing) as PdfRawStream;

                var decodedUnicodeCMap = toUnicode?.Decode(filterProvider);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = cMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap), isLenientParsing);
                }
            }

            return new TrueTypeSimpleFont(name, firstCharacter, lastCharacter, widths, descriptor, toUnicodeCMap);
        }

        private static int GetFirstCharacter(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType(CosName.FIRST_CHAR, out CosInt firstChar))
            {
                throw new InvalidFontFormatException(
                    $"No first character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return firstChar.AsInt();
        }

        private static int GetLastCharacter(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType(CosName.LAST_CHAR, out CosInt lastChar))
            {
                throw new InvalidFontFormatException(
                    $"No last character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return lastChar.AsInt();
        }

        private static decimal[] GetWidths(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType(CosName.WIDTHS, out COSArray widthArray))
            {
                throw new InvalidFontFormatException($"No widths array was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return widthArray.Select(x => ((ICosNumber)x).AsDecimal()).ToArray();
        }

        private FontDescriptor GetFontDescriptor(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (!dictionary.TryGetItemOfType(CosName.FONT_DESC, out CosObject obj))
            {
                throw new InvalidFontFormatException($"No font descriptor indirect reference found in the TrueType font: {dictionary}.");
            }

            var parsed = pdfObjectParser.Parse(obj.ToIndirectReference(), reader, isLenientParsing);

            if (!(parsed is PdfDictionary descriptorDictionary))
            {
                throw new InvalidFontFormatException($"Expected a font descriptor dictionary but instead found {parsed}.");
            }

            var descriptor = fontDescriptorFactory.Generate(descriptorDictionary, isLenientParsing);

            return descriptor;
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

            var fontFileStream = pdfObjectParser.Parse(descriptor.FontFile.ObjectKey, reader, isLenientParsing) as PdfRawStream;

            if (fontFileStream == null)
            {
                return null;
            }

            var fontFile = fontFileStream.Decode(filterProvider);

            var font = trueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile)));

            return font;
        }

        private static CosName GetName(PdfDictionary dictionary, FontDescriptor descriptor)
        {
            if (dictionary.TryGetName(CosName.BASE_FONT, out CosName name))
            {
                return name;
            }

            if (descriptor.FontName != null)
            {
                return descriptor.FontName;
            }

            throw new InvalidFontFormatException($"Could not find a name for this TrueType font {dictionary}.");
        }
    }
}
