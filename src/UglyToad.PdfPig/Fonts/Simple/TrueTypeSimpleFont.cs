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
        private readonly int firstCharacterCode;

        private readonly int lastCharacterCode;

        private readonly decimal[] widths;

        private readonly FontDescriptor descriptor;

        [CanBeNull]
        private readonly Encoding encoding;
        [CanBeNull]
        private readonly TrueTypeFont font;

        public NameToken Name { get; }

        public bool IsVertical { get; }

        [NotNull]
        public ToUnicodeCMap ToUnicode { get; set; }

        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        public TrueTypeSimpleFont(NameToken name, int firstCharacterCode, int lastCharacterCode, decimal[] widths,
            FontDescriptor descriptor,
            [CanBeNull] CMap toUnicodeCMap,
            [CanBeNull] Encoding encoding,
            [CanBeNull]TrueTypeFont font)
        {
            this.firstCharacterCode = firstCharacterCode;
            this.lastCharacterCode = lastCharacterCode;
            this.widths = widths;
            this.descriptor = descriptor;
            this.encoding = encoding;
            this.font = font;

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

        public PdfRectangle GetDisplacement(int characterCode)
        {
            return fontMatrix.Transform(GetRectangle(characterCode));
        }

        public PdfRectangle GetRectangle(int characterCode)
        {
            var index = characterCode - firstCharacterCode;

            if (index < 0 || index >= widths.Length)
            {
                return new PdfRectangle(0, 0, descriptor.MissingWidth, 0);
            }

            return new PdfRectangle(0, 0, widths[index], 0);
        }

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            if (font?.CMapTable == null)
            {
                return descriptor.BoundingBox;
            }

            if (!font.CMapTable.TryGetGlyphIndex(characterCode, out var index))
            {
                return descriptor.BoundingBox;
            }

            var glyph = font.GlyphTable.Glyphs[index];

            return glyph?.GlyphBounds ?? descriptor.BoundingBox;
        }

        public TransformationMatrix GetFontMatrix()
        {
            // TODO: should this also use units per em?
            return fontMatrix;
        }
    }
}
