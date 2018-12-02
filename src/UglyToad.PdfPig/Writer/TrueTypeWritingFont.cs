namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using Fonts.TrueType;
    using Geometry;
    using Tokens;

    internal class TrueTypeWritingFont : IWritingFont
    {
        private readonly TrueTypeFontProgram font;

        public TrueTypeWritingFont(TrueTypeFontProgram font)
        {
            this.font = font;
        }

        public bool HasWidths { get; } = true;

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            return font.TryGetBoundingBox(character, out boundingBox);
        }

        public IReadOnlyDictionary<IToken, IToken> GetDictionary(NameToken fontKeyName)
        {
            return new Dictionary<IToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.TrueType }
            };
        }
    }
}