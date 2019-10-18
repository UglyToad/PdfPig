namespace UglyToad.PdfPig.Fonts.Simple
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Composite;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokens;
    using TrueType;
    using Util.JetBrains.Annotations;

    internal class TrueTypeSimpleFont : IFont
    {
        private static readonly TransformationMatrix DefaultTransformation =
            TransformationMatrix.FromValues(1m / 1000m, 0, 0, 1m / 1000m, 0, 0);

        private readonly FontDescriptor descriptor;

        private readonly Dictionary<int, CharacterBoundingBox> boundingBoxCache
            = new Dictionary<int, CharacterBoundingBox>();

        [CanBeNull]
        private readonly Encoding encoding;

        [CanBeNull]
        private readonly TrueTypeFontProgram fontProgram;

        private readonly int firstCharacter;

        private readonly decimal[] widths;

        public NameToken Name { get; }

        public bool IsVertical { get; }

        [NotNull]
        public ToUnicodeCMap ToUnicode { get; set; }

        public TrueTypeSimpleFont(NameToken name,
            FontDescriptor descriptor,
            [CanBeNull] CMap toUnicodeCMap,
            [CanBeNull] Encoding encoding,
            [CanBeNull] TrueTypeFontProgram fontProgram,
            int firstCharacter,
            decimal[] widths)
        {
            this.descriptor = descriptor;
            this.encoding = encoding;
            this.fontProgram = fontProgram;
            this.firstCharacter = firstCharacter;
            this.widths = widths;

            Name = name;
            IsVertical = false;
            ToUnicode = new ToUnicodeCMap(toUnicodeCMap);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            value = null;

            // Behaviour specified by the Extraction of Text Content section of the specification.

            // If the font contains a ToUnicode CMap use that.
            if (ToUnicode.CanMapToUnicode)
            {
                return ToUnicode.TryGet(characterCode, out value);
            }

            if (encoding == null)
            {
                return false;
            }

            // If the font is a simple font that uses one of the predefined encodings MacRomanEncoding, MacExpertEncoding, or WinAnsiEncoding...

            //  Map the character code to a character name.
            var encodedCharacterName = encoding.GetName(characterCode);

            // Look up the character name in the Adobe Glyph List.
            try
            {
                value = GlyphList.AdobeGlyphList.NameToUnicode(encodedCharacterName);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (boundingBoxCache.TryGetValue(characterCode, out var cached))
            {
                return cached;
            }

            var fontMatrix = GetFontMatrix();

            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode, out var fromFont);

            var boundingBoxPreTransform = boundingBox.Width;

            if (fromFont)
            {
                boundingBox = fontMatrix.Transform(boundingBox);
            }
            else
            {
                boundingBox = DefaultTransformation.Transform(boundingBox);
            }

            decimal width;

            var index = characterCode - firstCharacter;
            if (widths != null && index >= 0 && index < widths.Length)
            {
                fromFont = false;
                width = widths[index];
            }
            else if (fontProgram != null)
            {
                if (!fontProgram.TryGetBoundingAdvancedWidth(characterCode, out width))
                {
                    width = boundingBoxPreTransform;
                }
            }
            else
            {
                throw new InvalidOperationException($"Could not retrieve width for character code: {characterCode} in font {Name}.");
            }

            if (fromFont)
            {
                width = fontMatrix.Transform(new PdfVector(width, 0)).X;
            }
            else
            {
                width = DefaultTransformation.Transform(new PdfVector(width, 0)).X;
            }

            var result = new CharacterBoundingBox(boundingBox, width);

            boundingBoxCache[characterCode] = result;

            return result;
        }

        public TransformationMatrix GetFontMatrix()
        {
            var scale = 1000m;

            if (fontProgram?.TableRegister.HeaderTable != null)
            {
                scale = fontProgram.GetFontMatrixMultiplier();
            }

            return TransformationMatrix.FromValues(1m / scale, 0, 0, 1m / scale, 0, 0);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode, out bool fromFont)
        {
            fromFont = true;

            if (fontProgram == null)
            {
                return descriptor.BoundingBox;
            }

            if (fontProgram.TryGetBoundingBox(characterCode, out var bounds))
            {
                return bounds;
            }

            if (fontProgram.TryGetBoundingAdvancedWidth(characterCode, out var width))
            {
                return new PdfRectangle(0, 0, width, 0);
            }

            fromFont = false;

            return new PdfRectangle(0, 0, GetWidth(characterCode), 0);
        }

        private decimal GetWidth(int characterCode)
        {
            var index = characterCode - firstCharacter;

            if (index < 0 || index >= widths.Length)
            {
                return descriptor.MissingWidth;
            }

            return widths[index];
        }
    }
}

