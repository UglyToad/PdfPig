namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
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

        public ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context)
        {
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type1  },
                { NameToken.BaseFont, NameToken.Create(metrics.FontName) },
                { NameToken.Encoding, NameToken.MacRomanEncoding },
                { NameToken.Name, fontKeyName }
            };

            var token = new DictionaryToken(dictionary);

            var result = context.WriteObject(outputStream, token);

            return result;
        }
    }

    internal class BuilderContext
    {
        public int CurrentNumber { get; private set; } = 1;

        private Dictionary<IndirectReference, long> objectOffsets = new Dictionary<IndirectReference, long>();
        public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets => objectOffsets;

        public ObjectToken WriteObject(Stream stream, IToken token)
        {
            var reference = new IndirectReference(CurrentNumber++, 0);
            var obj = new ObjectToken(stream.Position, reference, token);
            objectOffsets.Add(reference, obj.Position);
            TokenWriter.WriteToken(obj, stream);
            return obj;
        }
    }
}