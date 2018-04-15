using System;

namespace UglyToad.PdfPig.Fonts.Simple
{
    using Cmap;
    using Composite;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokenization.Tokens;
    using TrueType;
    using Util.JetBrains.Annotations;

    internal class TrueTypeSimpleFont : IFont
    {
        private readonly FontDescriptor descriptor;

        [CanBeNull]
        private readonly Encoding encoding;

        [CanBeNull]
        private readonly TrueTypeFont font;

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
            [CanBeNull] TrueTypeFont font,
            int firstCharacter,
            decimal[] widths)
        {
            this.descriptor = descriptor;
            this.encoding = encoding;
            this.font = font;
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

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            return GetFontMatrix().Transform(GetBoundingBoxInGlyphSpace(characterCode));
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (font == null)
            {
                return descriptor.BoundingBox;
            }

            if (font.TryGetBoundingBox(characterCode, out var bounds))
            {
                return bounds;
            }

            if (font.TryGetBoundingAdvancedWidth(characterCode, out var width))
            {
                return new PdfRectangle(0, 0, width, 0);
            }

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

        public TransformationMatrix GetFontMatrix()
        {
            var scale = 1000m;

            if (font?.HeaderTable != null)
            {
                scale = font.HeaderTable.UnitsPerEm;
            }

            return TransformationMatrix.FromValues(1m / scale, 0, 0, 1m / scale, 0, 0);
        }
    }
}
