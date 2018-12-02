namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using Fonts;
    using Fonts.Encodings;
    using Geometry;
    using Tokens;

    internal class Standard14WritingFont : IWritingFont
    {
        private readonly FontMetrics metrics;

        public bool HasWidths { get; } = false;

        public Standard14WritingFont(FontMetrics metrics)
        {
            this.metrics = metrics;
        }

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            var encoding = StandardEncoding.Instance;
            boundingBox = default(PdfRectangle);
            if (!metrics.CharacterMetrics.TryGetValue(encoding.GetName(character), out var characterMetric))
            {
                return false;
            }

            boundingBox = new PdfRectangle(characterMetric.BoundingBox.Left, characterMetric.BoundingBox.Bottom,
                characterMetric.BoundingBox.Left + characterMetric.WidthX, characterMetric.BoundingBox.Top);

            return true;
        }

        public IReadOnlyDictionary<IToken, IToken> GetDictionary(NameToken fontKeyName)
        {
            return new Dictionary<IToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type1  },
                { NameToken.BaseFont, NameToken.Create(metrics.FontName) },
                { NameToken.Encoding, NameToken.MacRomanEncoding },
                { NameToken.Name, fontKeyName }
            };
        }
    }
}