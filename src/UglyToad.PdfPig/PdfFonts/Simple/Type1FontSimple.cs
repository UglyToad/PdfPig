namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.Encodings;
    using Fonts.Type1;
    using Tokens;

    /// <summary>
    /// A font based on the Adobe Type 1 font format.
    /// </summary>
    internal sealed class Type1FontSimple : IFont
    {
        private static readonly TransformationMatrix DefaultTransformationMatrix = TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);

        private readonly Dictionary<int, CharacterBoundingBox> cachedBoundingBoxes = new Dictionary<int, CharacterBoundingBox>();

        private readonly int firstChar;

        private readonly int lastChar;

        private readonly double[] widths;

        private readonly FontDescriptor fontDescriptor;

        private readonly Encoding? encoding;

        private readonly Union<Type1Font, CompactFontFormatFontCollection>? fontProgram;

        private readonly ToUnicodeCMap toUnicodeCMap;

        private readonly TransformationMatrix fontMatrix;
        private readonly double ascent;
        private readonly double descent;
        
        private readonly bool isZapfDingbats;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; }

        public Type1FontSimple(
            NameToken name,
            int firstChar,
            int lastChar,
            double[] widths,
            FontDescriptor fontDescriptor,
            Encoding encoding,
            CMap toUnicodeCMap,
            Union<Type1Font, CompactFontFormatFontCollection> fontProgram)
        {
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.fontDescriptor = fontDescriptor;
            this.encoding = encoding;
            this.fontProgram = fontProgram;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);

            var matrix = DefaultTransformationMatrix;

            if (fontProgram is not null)
            {
                if (fontProgram.TryGetFirst(out var t1Font))
                {
                    matrix = t1Font.FontMatrix;
                }
                else if (fontProgram.TryGetSecond(out var cffFont))
                {
                    matrix = cffFont.GetFirstTransformationMatrix();
                }
            }

            fontMatrix = matrix;

            Name = name;
            Details = fontDescriptor?.ToDetails(name?.Data)
                      ?? FontDetails.GetDefault(name?.Data);
            isZapfDingbats = encoding is ZapfDingbatsEncoding || Details.Name.Contains("ZapfDingbats");
            descent = ComputeDescent();
            ascent = ComputeAscent();
        }

        private double ComputeDescent()
        {
            if (Math.Abs(fontDescriptor.Descent) > double.Epsilon)
            {
                return fontMatrix.TransformY(fontDescriptor.Descent);
            }

            /*
               // BobLd: Should 'fontProgram' be used
              if (fontProgram is not null)
              {
                  if (fontProgram.TryGetFirst(out var t1))
                  {

                  }

                  if (fontProgram.TryGetSecond(out var cffCol))
                  {

                  }
              }
              */

            return -0.25;
        }

        private double ComputeAscent()
        {
            if (Math.Abs(fontDescriptor.Ascent) > double.Epsilon)
            {
                return fontMatrix.TransformY(fontDescriptor.Ascent);
            }

            /*
             // BobLd: Should 'fontProgram' be used
            if (fontProgram is not null)
            {
                if (fontProgram.TryGetFirst(out var t1))
                {

                }

                if (fontProgram.TryGetSecond(out var cffCol))
                {

                }
            }
            */

            return 0.75;
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            value = null;
            if (toUnicodeCMap.CanMapToUnicode && toUnicodeCMap.TryGet(characterCode, out value))
            {
                return true;
            }

            if (encoding is null)
            {
                try
                {
                    value = char.ConvertFromUtf32(characterCode);
                    return true;
                }
                catch
                {
                    if (fontProgram == null)
                    {
                        return false;
                    }

                    var containsEncoding = false;
                    if (fontProgram.TryGetFirst(out var t1Font))
                    {
                        containsEncoding = t1Font.Encoding.TryGetValue(characterCode, out value);
                    }

                    return containsEncoding;
                }
            }

            var name = encoding.GetName(characterCode);

            try
            {
                value = NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return value is not null;
        }

        private string? NameToUnicode(string name)
        {
            if (isZapfDingbats)
            {
                string value = GlyphList.ZapfDingbats.NameToUnicode(name);
                if (value is not null)
                {
                    return value;
                }
            }

            return GlyphList.AdobeGlyphList.NameToUnicode(name);
        }

        public string UnicodeCodePointToName(int unicodeValue)
        {
            if (isZapfDingbats)
            {
                string value = GlyphList.ZapfDingbats.UnicodeCodePointToName(unicodeValue);
                if (value is not null)
                {
                    return value;
                }
            }

            return GlyphList.AdobeGlyphList.UnicodeCodePointToName(unicodeValue);
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (cachedBoundingBoxes.TryGetValue(characterCode, out var box))
            {
                return box;
            }

            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            var matrix = fontMatrix;

            boundingBox = matrix.Transform(boundingBox);

            var width = GetWidth(characterCode, boundingBox);

            var result = new CharacterBoundingBox(boundingBox, width / 1000.0);

            cachedBoundingBoxes[characterCode] = result;

            return result;
        }

        private double GetWidth(int characterCode, PdfRectangle boundingBox)
        {
            var widthIndex = characterCode - firstChar;

            if (widthIndex >= 0 && widthIndex < widths.Length)
            {
                return widths[widthIndex];
            }

            if (fontDescriptor?.MissingWidth != null)
            {
                return fontDescriptor.MissingWidth;
            }

            return boundingBox.Width;
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return new PdfRectangle(0, 0, 250, 0);
            }

            if (fontProgram == null)
            {
                return new PdfRectangle(0, 0, widths[characterCode - firstChar], 0);
            }

            PdfRectangle? rect = null;
            if (fontProgram.TryGetFirst(out var t1Font))
            {
                string name;
                if (encoding is not null)
                {
                    name = encoding.GetName(characterCode);
                }
                else
                {
                    name = UnicodeCodePointToName(characterCode);
                }
                
                rect = t1Font.GetCharacterBoundingBox(name);
            }
            else if (fontProgram.TryGetSecond(out var cffFont))
            {
                var first = cffFont.FirstFont;
                string characterName;
                if (encoding is not null)
                {
                    characterName = encoding.GetName(characterCode);
                }
                else
                {
                    characterName = cffFont.GetCharacterName(characterCode, false);
                }

                rect = first.GetCharacterBoundingBox(characterName);
            }

            if (!rect.HasValue)
            {
                return new PdfRectangle(0, 0, widths[characterCode - firstChar], 0);
            }

            // ReSharper disable once PossibleInvalidOperationException
            return rect.Value;
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        public double GetDescent()
        {
            return descent;
        }

        public double GetAscent()
        {
            return ascent;
        }

        /// <inheritdoc/>
        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;
            IReadOnlyList<PdfSubpath>? tempPath = null;
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return false;
            }

            if (fontProgram is null)
            {
                return false;
            }

            if (fontProgram.TryGetFirst(out var t1Font))
            {
                string name;
                if (encoding is not null)
                {
                    name = encoding.GetName(characterCode);
                }
                else
                {
                    name = UnicodeCodePointToName(characterCode);
                }
                
                tempPath = t1Font.GetCharacterPath(name);
            }
            else if (fontProgram.TryGetSecond(out var cffFont))
            {
                var first = cffFont.FirstFont;
                string characterName;
                if (encoding is not null)
                {
                    characterName = encoding.GetName(characterCode);
                }
                else
                {
                    characterName = cffFont.GetCharacterName(characterCode, false);
                }

                tempPath = first.GetCharacterPath(characterName);
            }

            if (tempPath is not null)
            {
                path = tempPath;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            if (TryGetPath(characterCode, out path))
            {
                path = fontMatrix.Transform(path).ToArray();
                return true;
            }
            return false;
        }
    }
}
