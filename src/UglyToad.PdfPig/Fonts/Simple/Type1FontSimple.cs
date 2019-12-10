namespace UglyToad.PdfPig.Fonts.Simple
{
    using System.Collections.Generic;
    using Cmap;
    using CompactFontFormat;
    using Composite;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokens;
    using Type1;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A font based on the Adobe Type 1 font format.
    /// </summary>
    internal class Type1FontSimple : IFont
    {
        private readonly Dictionary<int, CharacterBoundingBox> cachedBoundingBoxes = new Dictionary<int, CharacterBoundingBox>();

        private readonly int firstChar;

        private readonly int lastChar;

        private readonly decimal[] widths;

        private readonly FontDescriptor fontDescriptor;

        private readonly Encoding encoding;

        [CanBeNull]
        private readonly Union<Type1FontProgram, CompactFontFormatFontProgram> fontProgram;

        private readonly ToUnicodeCMap toUnicodeCMap;

        private readonly TransformationMatrix fontMatrix;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public Type1FontSimple(NameToken name, int firstChar, int lastChar, decimal[] widths, FontDescriptor fontDescriptor, Encoding encoding, 
            CMap toUnicodeCMap,
            Union<Type1FontProgram, CompactFontFormatFontProgram> fontProgram)
        {
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.fontDescriptor = fontDescriptor;
            this.encoding = encoding;
            this.fontProgram = fontProgram;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);

            var matrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);
            fontProgram?.Match(x => matrix = x.GetFontTransformationMatrix(), x => { matrix = x.GetFontTransformationMatrix(); });

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

            var width = matrix.TransformX(GetWidth(characterCode, boundingBox));

            var result = new CharacterBoundingBox(boundingBox, width);

            cachedBoundingBoxes[characterCode] = result;

            return result;
        }

        private decimal GetWidth(int characterCode, PdfRectangle boundingBox)
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

            var rect = fontProgram.Match(x =>
                {
                    var name = encoding.GetName(characterCode);
                    return x.GetCharacterBoundingBox(name);
                },
                x =>
                {
                    string characterName;
                    if (encoding != null)
                    {
                        characterName = encoding.GetName(characterCode);
                    }
                    else
                    {
                        characterName = x.GetCharacterName(characterCode);
                    }

                    return x.GetCharacterBoundingBox(characterName);
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
