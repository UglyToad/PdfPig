namespace UglyToad.PdfPig.Fonts.Parser.Parts
{
    using System;
    using Geometry;
    using Tokenization.Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    internal class FontDescriptorFactory
    {
        public FontDescriptor Generate(DictionaryToken dictionary, bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var name = GetFontName(dictionary, isLenientParsing);
            var family = GetFontFamily(dictionary);
            var stretch = GetFontStretch(dictionary);
            var flags = GetFlags(dictionary, isLenientParsing);
            var bounding = GetBoundingBox(dictionary);
            var charSet = GetCharSet(dictionary);
            var fontFile = GetFontFile(dictionary);

            return new FontDescriptor(name, flags)
            {
                 FontFamily = family,
                 Stretch = stretch,
                 FontWeight = GetDecimalOrDefault(dictionary, NameToken.FontWeight),
                 BoundingBox = bounding,
                 ItalicAngle = GetDecimalOrDefault(dictionary, NameToken.ItalicAngle),
                 Ascent = GetDecimalOrDefault(dictionary, NameToken.Ascent),
                 Descent = GetDecimalOrDefault(dictionary, NameToken.Descent),
                 Leading = GetDecimalOrDefault(dictionary, NameToken.Leading),
                 CapHeight = Math.Abs(GetDecimalOrDefault(dictionary, NameToken.CapHeight)),
                 XHeight = Math.Abs(GetDecimalOrDefault(dictionary, NameToken.Xheight)),
                 StemVertical = GetDecimalOrDefault(dictionary, NameToken.StemV),
                 StemHorizontal = GetDecimalOrDefault(dictionary, NameToken.StemH),
                 AverageWidth = GetDecimalOrDefault(dictionary, NameToken.AvgWidth),
                 MaxWidth = GetDecimalOrDefault(dictionary, NameToken.MaxWidth),
                 MissingWidth = GetDecimalOrDefault(dictionary, NameToken.MissingWidth),
                 FontFile = fontFile,
                 CharSet = charSet
            };
        }

        private static decimal GetDecimalOrDefault(DictionaryToken dictionary, NameToken name)
        {
            if (!dictionary.TryGet(name, out var token) || !(token is NumericToken number))
            {
                return 0;
            }

            return number.Data;
        }

        private static NameToken GetFontName(DictionaryToken dictionary, bool isLenientParsing)
        {
            if (!dictionary.TryGet(NameToken.FontName, out var name) || !(name is NameToken nameToken))
            {
                if (isLenientParsing)
                {
                    nameToken = NameToken.Create(string.Empty);
                }
                else
                {
                    throw new InvalidOperationException("Could not parse the font descriptor, could not retrieve the font name. " + dictionary);
                }
            }

            return nameToken;
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

        private static FontFlags GetFlags(DictionaryToken dictionary, bool isLenientParsing)
        {
            var flags = dictionary.GetIntOrDefault(NameToken.Flags, -1);

            if (flags == -1)
            {
                if (isLenientParsing)
                {
                    flags = 0;
                }
                else
                {
                    throw new InvalidOperationException("Font flags were not set correctly for the font descriptor: " + dictionary);
                }
            }

            return (FontFlags) flags;
        }

        private static PdfRectangle GetBoundingBox(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.FontBbox, out var box) || !(box is ArrayToken boxArray))
            {
                return new PdfRectangle(0, 0, 0, 0);
            }

            if (boxArray.Data.Count != 4)
            {
                return new PdfRectangle(0, 0, 0, 0);
            }
            var x1 = boxArray.GetNumeric(0).Data;
            var y1 = boxArray.GetNumeric(1).Data;
            var x2 = boxArray.GetNumeric(2).Data;
            var y2 = boxArray.GetNumeric(3).Data;
            
            return new PdfRectangle(x1, y1, x2, y2);
        }

        private static string GetCharSet(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.CharSet, out var set) || !(set is NameToken setName))
            {
                return null;
            }

            return setName.Data;
        }

        [CanBeNull]
        private static DescriptorFontFile GetFontFile(DictionaryToken dictionary)
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

