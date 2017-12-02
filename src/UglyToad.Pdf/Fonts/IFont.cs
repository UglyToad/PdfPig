namespace UglyToad.Pdf.Fonts
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Cos;
    using Geometry;
    using IO;

    internal interface IFont
    {
        CosName Name { get; }

        CosName SubType { get; }

        string BaseFontType { get; }

        bool IsVertical { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        string GetUnicode(int characterCode);

        PdfVector GetDisplacement(int characterCode);
    }

    internal class CompositeFont : IFont
    {
        private readonly Dictionary<int, decimal> codeToWidthMap = new Dictionary<int, decimal>();

        public CosName Name { get; set; }

        public CosName SubType { get; set; }

        public string BaseFontType { get; }

        public bool IsVertical { get; }

        public CMap ToUnicode { get; set; }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            var current = bytes.CurrentOffset;

            var code = ToUnicode.ReadCode(bytes);

            codeLength = bytes.CurrentOffset - current;

            return code;
        }

        public string GetUnicode(int characterCode)
        {
            if (ToUnicode != null)
            {
                if (ToUnicode.TryConvertToUnicode(characterCode, out string s)) return s;
            }

            throw new NotImplementedException($"Could not locate the unicode for the character code {characterCode} in font {Name}.");
        }

        public PdfVector GetDisplacement(int characterCode)
        {
            var width = GetCharacterWidth(characterCode);
            return new PdfVector(width / 1000, 0);
        }

        private decimal GetCharacterWidth(int characterCode)
        {
            if (codeToWidthMap.TryGetValue(characterCode, out var width))
            {
                return width;
            }

            return 12000;
        }
    }
}
