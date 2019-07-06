namespace UglyToad.PdfPig.Fonts.Simple
{
    using System;
    using Core;
    using Encodings;
    using IO;
    using Tokens;
    using TrueType;

    /// <summary>
    /// Some TrueType fonts use both the Standard 14 descriptor and the TrueType font from disk.
    /// </summary>
    internal class TrueTypeStandard14FallbackSimpleFont : IFont
    {
        private static readonly TransformationMatrix DefaultTransformation =
            TransformationMatrix.FromValues(1m / 1000m, 0, 0, 1m / 1000m, 0, 0);

        private readonly FontMetrics fontMetrics;
        private readonly Encoding encoding;
        private readonly TrueTypeFontProgram font;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public TrueTypeStandard14FallbackSimpleFont(NameToken name, FontMetrics fontMetrics, Encoding encoding, TrueTypeFontProgram font)
        {
            this.fontMetrics = fontMetrics;
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            this.font = font;
            Name = name;
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            value = null;

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
            var fontMatrix = GetFontMatrix();
            if (font != null && font.TryGetBoundingBox(characterCode, out var bounds))
            {
                bounds = fontMatrix.Transform(bounds);
                return new CharacterBoundingBox(bounds, bounds.Width);
            }

            var name = encoding.GetName(characterCode);
            var metrics = fontMetrics.CharacterMetrics[name];

            bounds = fontMatrix.Transform(metrics.BoundingBox);
            var width = fontMatrix.TransformX(metrics.WidthX);

            return new CharacterBoundingBox(bounds, width);
        }

        public TransformationMatrix GetFontMatrix()
        {
            if (font?.TableRegister.HeaderTable != null)
            {
                var scale = (decimal)font.GetFontMatrixMultiplier();

                return TransformationMatrix.FromValues(1 / scale, 0, 0, 1 / scale, 0, 0);
            }

            return DefaultTransformation;
        }
    }
}