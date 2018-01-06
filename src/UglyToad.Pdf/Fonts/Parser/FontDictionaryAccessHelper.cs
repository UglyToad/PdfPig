namespace UglyToad.Pdf.Fonts.Parser
{
    using System.Linq;
    using ContentStream;
    using Cos;
    using Exceptions;
    using IO;
    using Parts;
    using Pdf.Parser;
    using Pdf.Parser.Parts;

    internal static class FontDictionaryAccessHelper
    {
        public static int GetFirstCharacter(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType(CosName.FIRST_CHAR, out CosInt firstChar))
            {
                throw new InvalidFontFormatException($"No first character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return firstChar.AsInt();
        }

        public static int GetLastCharacter(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType(CosName.LAST_CHAR, out CosInt lastChar))
            {
                throw new InvalidFontFormatException($"No last character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return lastChar.AsInt();
        }

        public static decimal[] GetWidths(IPdfObjectParser pdfObjectParser, PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (!dictionary.TryGetItemOfType(CosName.WIDTHS, out COSArray widthArray))
            {
                if (!dictionary.TryGetItemOfType(CosName.WIDTHS, out CosObject arr))
                {
                    throw new InvalidFontFormatException($"No widths array was found in the font dictionary for this TrueType font: {dictionary}.");
                }

                widthArray = DirectObjectFinder.Find<COSArray>(arr, pdfObjectParser, reader, isLenientParsing);
            }

            return widthArray.Select(x => ((ICosNumber)x).AsDecimal()).ToArray();
        }

        public static FontDescriptor GetFontDescriptor(IPdfObjectParser pdfObjectParser, FontDescriptorFactory fontDescriptorFactory, PdfDictionary dictionary, 
            IRandomAccessRead reader, bool isLenientParsing)
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
        
        public static CosName GetName(PdfDictionary dictionary, FontDescriptor descriptor)
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
