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
        private static readonly TransformationMatrix FontMatrix =
            TransformationMatrix.FromValues(1/1000m, 0, 0, 1/1000m, 0, 0);
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

        public PdfVector GetDisplacement(int characterCode)
        {
            var tx = GetWidth(characterCode);

            return new PdfVector(tx / 1000m, 0);
        }

        public decimal GetWidth(int characterCode)
        {
            var index = characterCode - firstCharacterCode;
            
            if (index < 0 || index >= widths.Length)
            {
                return descriptor.MissingWidth;
            }

            return widths[index];
        }

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            throw new System.NotImplementedException();
        }

        public TransformationMatrix GetFontMatrix()
        {
            // TODO: should this also use units per em?
            return FontMatrix;
        }
    }
}
