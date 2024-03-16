namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using System;
    using Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    internal static class FontDescriptorFactory
    {
        public static FontDescriptor Generate(DictionaryToken dictionary, IPdfTokenScanner pdfScanner)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var name = GetFontName(dictionary, pdfScanner);
            var family = GetFontFamily(dictionary);
            var stretch = GetFontStretch(dictionary);
            var flags = GetFlags(dictionary);
            var bounding = GetBoundingBox(dictionary, pdfScanner);
            var charSet = GetCharSet(dictionary);
            var fontFile = GetFontFile(dictionary);

            return new FontDescriptor.Builder(name, flags)
            {
                 FontFamily = family,
                 Stretch = stretch,
                 FontWeight = GetDoubleOrDefault(dictionary, NameToken.FontWeight),
                 BoundingBox = bounding,
                 ItalicAngle = GetDoubleOrDefault(dictionary, NameToken.ItalicAngle),
                 Ascent = GetDoubleOrDefault(dictionary, NameToken.Ascent),
                 Descent = GetDoubleOrDefault(dictionary, NameToken.Descent),
                 Leading = GetDoubleOrDefault(dictionary, NameToken.Leading),
                 CapHeight = Math.Abs(GetDoubleOrDefault(dictionary, NameToken.CapHeight)),
                 XHeight = Math.Abs(GetDoubleOrDefault(dictionary, NameToken.Xheight)),
                 StemVertical = GetDoubleOrDefault(dictionary, NameToken.StemV),
                 StemHorizontal = GetDoubleOrDefault(dictionary, NameToken.StemH),
                 AverageWidth = GetDoubleOrDefault(dictionary, NameToken.AvgWidth),
                 MaxWidth = GetDoubleOrDefault(dictionary, NameToken.MaxWidth),
                 MissingWidth = GetDoubleOrDefault(dictionary, NameToken.MissingWidth),
                 FontFile = fontFile,
                 CharSet = charSet
            }.Build();
        }

        private static double GetDoubleOrDefault(DictionaryToken dictionary, NameToken name)
        {
            if (!dictionary.TryGet(name, out var token) || !(token is NumericToken number))
            {
                return 0;
            }

            return number.Data;
        }

        private static NameToken GetFontName(DictionaryToken dictionary, IPdfTokenScanner scanner)
        {
            if (!dictionary.TryGet(NameToken.FontName, scanner, out NameToken? name))
            {
                name = NameToken.Create(string.Empty);
            }

            return name;
        }

        private static string GetFontFamily(DictionaryToken dictionary)
        {
            if (dictionary.TryGet(NameToken.FontFamily, out var value) && value is StringToken fontFamily)
            {
                return fontFamily.Data;
            }

            return string.Empty;
        }

        private static FontStretch GetFontStretch(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontStretch, out var stretch) || !(stretch is NameToken stretchName))
            {
                return FontStretch.Normal;
            }

            return stretchName.ConvertToFontStretch();
        }

        private static FontDescriptorFlags GetFlags(DictionaryToken dictionary)
        {
            var flags = dictionary.GetIntOrDefault(NameToken.Flags, -1);

            if (flags == -1)
            {
                flags = 0;
            }

            return (FontDescriptorFlags) flags;
        }

        private static PdfRectangle GetBoundingBox(DictionaryToken dictionary, IPdfTokenScanner pdfScanner)
        {
            if (!dictionary.TryGet(NameToken.FontBbox, out var box) || !(box is ArrayToken boxArray))
            {
                return new PdfRectangle(0, 0, 0, 0);
            }

            if (boxArray.Data.Count != 4)
            {
                return new PdfRectangle(0, 0, 0, 0);
            }

            return boxArray.ToRectangle(pdfScanner);
        }

        private static string? GetCharSet(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.CharSet, out var set) || !(set is NameToken setName))
            {
                return null;
            }

            return setName.Data;
        }

        private static DescriptorFontFile? GetFontFile(DictionaryToken dictionary)
        {
            if (dictionary.TryGet(NameToken.FontFile, out var value))
            {
                if (!(value is IndirectReferenceToken obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile to be an object reference.");
                }

                return new DescriptorFontFile(obj, DescriptorFontFile.FontFileType.Type1);
            }

            if (dictionary.TryGet(NameToken.FontFile2, out value))
            {
                if (!(value is IndirectReferenceToken obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile2 to be an object reference.");
                }

                return new DescriptorFontFile(obj, DescriptorFontFile.FontFileType.TrueType);
            }

            if (dictionary.TryGet(NameToken.FontFile3, out value))
            {
                if (!(value is IndirectReferenceToken obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile3 to be an object reference.");
                }

                return new DescriptorFontFile(obj, DescriptorFontFile.FontFileType.FromSubtype);
            }

            return null;
        }
    }
}

