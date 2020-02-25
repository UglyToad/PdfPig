namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System.Collections.Generic;
    using System.Linq;
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.Encodings;
    using Fonts.Type1;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A font based on the Adobe Type 1 font format.
    /// </summary>
    internal class Type1FontSimple : IFont
    {
        private readonly Dictionary<int, CharacterBoundingBox> cachedBoundingBoxes = new Dictionary<int, CharacterBoundingBox>();

        private readonly int firstChar;

        private readonly int lastChar;

        private readonly double[] widths;

        private readonly FontDescriptor fontDescriptor;

        private readonly Encoding encoding;

        [CanBeNull]
        private readonly Union<Type1Font, CompactFontFormatFontCollection> fontProgram;

        private readonly ToUnicodeCMap toUnicodeCMap;

        private readonly TransformationMatrix fontMatrix;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public Type1FontSimple(NameToken name, int firstChar, int lastChar, double[] widths, FontDescriptor fontDescriptor, Encoding encoding, 
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

            var matrix = TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);
            fontProgram?.Match(x => matrix = x.FontMatrix, x => { matrix = x.GetFirstTransformationMatrix(); });

            fontMatrix = matrix;

            Name = name;
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            if (toUnicodeCMap.CanMapToUnicode)
            {
                return toUnicodeCMap.TryGet(characterCode, out value);
            }

            value = null;

            if (encoding == null)
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
                    var capturedValue = default(string);
                    fontProgram.Match(x => { containsEncoding = x.Encoding.TryGetValue(characterCode, out capturedValue); },
                        _ => {});
                    value = capturedValue;
                    return containsEncoding;
                }
            }

            var name = encoding.GetName(characterCode);
            
            try
            {
                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return true;
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

            var result = new CharacterBoundingBox(boundingBox, width/1000.0);

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
                return (double)fontDescriptor.MissingWidth;
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

            var rect = fontProgram.Match(x =>
                {
                    var name = encoding.GetName(characterCode);
                    return x.GetCharacterBoundingBox(name);
                },
                x =>
                {
                    var first = x.Fonts.First().Value;
                    string characterName;
                    if (encoding != null)
                    {
                        characterName = encoding.GetName(characterCode);
                    }
                    else
                    {
                        characterName = x.GetCharacterName(characterCode);
                    }

                    return first.GetCharacterBoundingBox(characterName);
                });

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
    }
}
