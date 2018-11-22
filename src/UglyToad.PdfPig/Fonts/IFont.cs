namespace UglyToad.PdfPig.Fonts
{
    using Core;
    using Geometry;
    using IO;
    using Tokens;

    internal interface IFont
    {
        NameToken Name { get; }
        
        bool IsVertical { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        bool TryGetUnicode(int characterCode, out string value);

        CharacterBoundingBox GetBoundingBox(int characterCode);

        TransformationMatrix GetFontMatrix();
    }

    internal class CharacterBoundingBox
    {
        public PdfRectangle GlyphBounds { get; }

        public decimal Width { get; }

        public CharacterBoundingBox(PdfRectangle glyphBounds, decimal width)
        {
            GlyphBounds = glyphBounds;
            Width = width;
        }
    }
}
