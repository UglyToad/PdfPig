namespace UglyToad.PdfPig.Fonts.Parser.Parts
{
    using System;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Geometry;
    using Util.JetBrains.Annotations;

    internal class FontDescriptorFactory
    {
        public FontDescriptor Generate(PdfDictionary dictionary, bool isLenientParsing)
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
                 FontWeight = dictionary.GetDecimalOrDefault(CosName.FONT_WEIGHT, 0),
                 BoundingBox = bounding,
                 ItalicAngle = dictionary.GetDecimalOrDefault(CosName.ITALIC_ANGLE, 0),
                 Ascent = dictionary.GetDecimalOrDefault(CosName.ASCENT, 0),
                 Descent = dictionary.GetDecimalOrDefault(CosName.DESCENT, 0),
                 Leading = dictionary.GetDecimalOrDefault(CosName.LEADING, 0),
                 CapHeight = Math.Abs(dictionary.GetDecimalOrDefault(CosName.CAP_HEIGHT, 0)),
                 XHeight = Math.Abs(dictionary.GetDecimalOrDefault(CosName.XHEIGHT, 0)),
                 StemVertical = dictionary.GetDecimalOrDefault(CosName.STEM_V, 0),
                 StemHorizontal = dictionary.GetDecimalOrDefault(CosName.STEM_H, 0),
                 AverageWidth = dictionary.GetDecimalOrDefault(CosName.AVG_WIDTH, 0),
                 MaxWidth = dictionary.GetDecimalOrDefault(CosName.MAX_WIDTH, 0),
                 MissingWidth = dictionary.GetDecimalOrDefault(CosName.MISSING_WIDTH, 0),
                 FontFile = fontFile,
                 CharSet = charSet
            };
        }

        private static CosName GetFontName(PdfDictionary dictionary, bool isLenientParsing)
        {
            if (!dictionary.TryGetName(CosName.FONT_NAME, out var name))
            {
                if (isLenientParsing)
                {
                    name = CosName.Create(string.Empty);
                }
                else
                {
                    throw new InvalidOperationException("Could not parse the font descriptor, could not retrieve the font name. " + dictionary);
                }
            }

            return name;
        }

        private static string GetFontFamily(PdfDictionary dictionary)
        {
            if (dictionary.TryGetItemOfType<CosString>(CosName.FONT_FAMILY, out var value))
            {
                return value.GetString();
            }

            return string.Empty;
        }

        private static FontStretch GetFontStretch(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetName(CosName.FONT_STRETCH, out var stretch))
            {
                return FontStretch.Normal;
            }

            return stretch.ConvertToFontStretch();
        }

        private static FontFlags GetFlags(PdfDictionary dictionary, bool isLenientParsing)
        {
            var flags = dictionary.GetIntOrDefault(CosName.FLAGS, -1);

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

        private static PdfRectangle GetBoundingBox(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetItemOfType<COSArray>(CosName.FONT_BBOX, out var box))
            {
                return new PdfRectangle(0, 0, 0, 0);
            }

            if (box.Count != 4)
            {
                return new PdfRectangle(0, 0, 0, 0);
            }
            var x1 = box.getInt(0);
            var y1 = box.getInt(1);
            var x2 = box.getInt(2);
            var y2 = box.getInt(3);
            
            return new PdfRectangle(x1, y1, x2, y2);
        }

        private static string GetCharSet(PdfDictionary dictionary)
        {
            if (!dictionary.TryGetName(CosName.CHAR_SET, out var set))
            {
                return null;
            }

            return set.Name;
        }

        [CanBeNull]
        private static DescriptorFontFile GetFontFile(PdfDictionary dictionary)
        {
            if (dictionary.TryGetValue(CosName.FONT_FILE, out var value))
            {
                if (!(value is CosObject obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile to be an object reference.");
                }

                return new DescriptorFontFile(obj.ToIndirectReference(), DescriptorFontFile.FontFileType.Type1);
            }

            if (dictionary.TryGetValue(CosName.FONT_FILE2, out value))
            {
                if (!(value is CosObject obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile2 to be an object reference.");
                }

                return new DescriptorFontFile(obj.ToIndirectReference(), DescriptorFontFile.FontFileType.TrueType);
            }

            if (dictionary.TryGetValue(CosName.FONT_FILE3, out value))
            {
                if (!(value is CosObject obj))
                {
                    throw new NotSupportedException("We currently expect the FontFile3 to be an object reference.");
                }

                return new DescriptorFontFile(obj.ToIndirectReference(), DescriptorFontFile.FontFileType.FromSubtype);
            }

            return null;
        }
    }
}

