namespace UglyToad.Pdf.Fonts.Simple
{
    using Cmap;
    using Composite;
    using Cos;
    using Geometry;
    using IO;
    using Util.JetBrains.Annotations;

    internal class TrueTypeSimpleFont : IFont
    {
        private readonly int firstCharacterCode;
        private readonly int lastCharacterCode;
        private readonly decimal[] widths;
        private readonly FontDescriptor descriptor;

        public CosName Name { get; }

        public bool IsVertical { get; }

        [NotNull]
        public ToUnicodeCMap ToUnicode { get; set; }

        public TrueTypeSimpleFont(CosName name, int firstCharacterCode, int lastCharacterCode, decimal[] widths, 
            FontDescriptor descriptor,
            [CanBeNull]CMap toUnicodeCMap)
        {
            this.firstCharacterCode = firstCharacterCode;
            this.lastCharacterCode = lastCharacterCode;
            this.widths = widths;
            this.descriptor = descriptor;

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

            if (!ToUnicode.CanMapToUnicode)
            {
                return false;
            }

            return ToUnicode.TryGet(characterCode, out value);
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
    }
}
