namespace UglyToad.PdfPig.Fonts.Simple
{
    using System;
    using Core;
    using Encodings;
    using Geometry;
    using IO;
    using Tokens;

    /// <summary>
    /// A font using one of the Adobe Standard 14 fonts. Can use a custom encoding.
    /// </summary>
    internal class Type1Standard14Font: IFont
    {
        private readonly FontMetrics standardFontMetrics;
        private readonly Encoding encoding;

        public NameToken Name { get; }
        public bool IsVertical { get; }

        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        public Type1Standard14Font(FontMetrics standardFontMetrics, Encoding overrideEncoding = null)
        {
            this.standardFontMetrics = standardFontMetrics ?? throw new ArgumentNullException(nameof(standardFontMetrics));
            encoding = overrideEncoding ?? new AdobeFontMetricsEncoding(standardFontMetrics);

            Name = NameToken.Create(standardFontMetrics.FontName);
            
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

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            boundingBox = fontMatrix.Transform(boundingBox);

            return new CharacterBoundingBox(boundingBox, boundingBox.Width);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            var name = encoding.GetName(characterCode);

            if (!standardFontMetrics.CharacterMetrics.TryGetValue(name, out var metrics))
            {
                return new PdfRectangle(0, 0, 250, 0);
            }

            return new PdfRectangle(0, 0, metrics.WidthX, metrics.WidthY);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }
    }
}
