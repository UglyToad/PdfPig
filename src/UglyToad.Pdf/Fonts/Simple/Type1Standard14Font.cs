namespace UglyToad.Pdf.Fonts.Simple
{
    using System;
    using Core;
    using Cos;
    using Encodings;
    using Geometry;
    using IO;

    internal class Type1Standard14Font: IFont
    {
        private static readonly TransformationMatrix FontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        private readonly FontMetrics standardFontMetrics;
        private readonly Encoding encoding;

        public CosName Name { get; }
        public bool IsVertical { get; }

        public Type1Standard14Font(FontMetrics standardFontMetrics)
        {
            this.standardFontMetrics = standardFontMetrics ?? throw new ArgumentNullException(nameof(standardFontMetrics));
            encoding = new AdobeFontMetricsEncoding(standardFontMetrics);

            Name = CosName.Create(standardFontMetrics.FontName);
            
            IsVertical = false;
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            var name = encoding.GetName(characterCode);

            var listed = GlyphList.AdobeGlyphList.NameToUnicode(name);

            value = listed;

            return true;
        }

        public PdfVector GetDisplacement(int characterCode)
        {
            return FontMatrix.Transform(new PdfVector(GetWidth(characterCode), 0));
        }

        public decimal GetWidth(int characterCode)
        {
            var name = encoding.GetName(characterCode);

            if (!standardFontMetrics.CharacterMetrics.TryGetValue(name, out var metrics))
            {
                return 250;
            }

            return metrics.WidthX;
        }

        public TransformationMatrix GetFontMatrix()
        {
            return FontMatrix;
        }
    }
}
