namespace UglyToad.PdfPig.Fonts.Simple
{
    using Cmap;
    using Composite;
    using Core;
    using Cos;
    using Encodings;
    using Geometry;
    using IO;

    /// <summary>
    /// TODO: implement this properly if you find a Type 1 font in the wild.
    /// </summary>
    internal class Type1Font : IFont
    {
        private readonly int firstChar;
        private readonly int lastChar;
        private readonly decimal[] widths;
        private readonly FontDescriptor fontDescriptor;
        private readonly Encoding encoding;
        private readonly ToUnicodeCMap toUnicodeCMap;
        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        public CosName Name { get; }

        public bool IsVertical { get; } = false;

        public Type1Font(CosName name, int firstChar, int lastChar, decimal[] widths, FontDescriptor fontDescriptor, Encoding encoding, CMap toUnicodeCMap)
        {
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.fontDescriptor = fontDescriptor;
            this.encoding = encoding;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);
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
                return false;
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

        public PdfVector GetDisplacement(int characterCode)
        {
            return fontMatrix.Transform(new PdfVector(GetWidth(characterCode), 0));
        }

        public decimal GetWidth(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                return 250;
            }

            return widths[characterCode - firstChar];
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }
    }
}
