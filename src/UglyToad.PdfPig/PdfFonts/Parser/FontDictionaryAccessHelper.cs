namespace UglyToad.PdfPig.PdfFonts.Parser
{
    using Fonts;
    using Parts;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class FontDictionaryAccessHelper
    {
        public static int GetFirstCharacter(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FirstChar, out var firstChar) || !(firstChar is NumericToken number))
            {
                throw new InvalidFontFormatException($"No first character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return number.Int;
        }

        public static int GetLastCharacter(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.LastChar, out var firstChar) || !(firstChar is NumericToken number))
            {
                throw new InvalidFontFormatException($"No first character entry was found in the font dictionary for this TrueType font: {dictionary}.");
            }

            return number.Int;
        }

        public static double[] GetWidths(IPdfTokenScanner pdfScanner, DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.Widths, out var token))
            {
                throw new InvalidFontFormatException($"No widths array found for the font: {dictionary}.");
            }

            var widthArray = DirectObjectFinder.Get<ArrayToken>(token, pdfScanner);
            if (widthArray is null)
            {
                return [];
            }
            
            var result = new double[widthArray.Data.Count];
            for (int i = 0; i < widthArray.Data.Count; i++)
            {
                var arrayToken = widthArray.Data[i];

                if (!(arrayToken is NumericToken number))
                {
                    if (DirectObjectFinder.TryGet<NumericToken>(arrayToken, pdfScanner, out var resolved))
                    {
                        number = resolved;
                    }
                    else
                    {
                        throw new InvalidFontFormatException($"Token which was not a number found in the widths array: {arrayToken}.");
                    }
                }

                result[i] = number.Double;
            }

            return result;
        }

        public static FontDescriptor GetFontDescriptor(IPdfTokenScanner pdfScanner, DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontDescriptor, pdfScanner, out DictionaryToken? parsed))
            {
                throw new InvalidFontFormatException($"No font descriptor indirect reference found in the TrueType font: {dictionary}.");
            }
            
            var descriptor = FontDescriptorFactory.Generate(parsed, pdfScanner);

            return descriptor;
        }
        
        public static NameToken GetName(IPdfTokenScanner pdfScanner, DictionaryToken dictionary, FontDescriptor descriptor)
        {
            if (dictionary.TryGet(NameToken.BaseFont, out var nameBase))
            {
                var name = DirectObjectFinder.Get<NameToken>(nameBase, pdfScanner);
                if (name is not null)
                {
                    return name;
                }
            }

            if (descriptor.FontName is not null)
            {
                return descriptor.FontName;
            }
            
            throw new InvalidFontFormatException($"Could not find a name for this font {dictionary}.");
        }
    }
}
