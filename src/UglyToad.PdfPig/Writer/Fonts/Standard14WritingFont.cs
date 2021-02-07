namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using PdfPig.Fonts;
    using PdfPig.Fonts.AdobeFontMetrics;
    using PdfPig.Fonts.Encodings;
    using Tokens;
    using Util.JetBrains.Annotations;

    internal class Standard14WritingFont : IWritingFont
    {
        private readonly AdobeFontMetrics metrics;

        public bool HasWidths { get; } = false;

        public string Name => metrics.FontName;

        public Standard14WritingFont(AdobeFontMetrics metrics)
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
                characterMetric.BoundingBox.Left + characterMetric.Width.X, characterMetric.BoundingBox.Top);

            return true;
        }

        public bool TryGetAdvanceWidth(char character, out double width)
        {
            width = 0;
            if (!TryGetBoundingBox(character, out var bbox))
            {
                return false;
            }

            width = bbox.Width;

            return true;
        }

        public TransformationMatrix GetFontMatrix()
        {
            return TransformationMatrix.FromValues(1/1000.0, 0, 0, 1/1000.0, 0, 0);
        }

        public IndirectReferenceToken WriteFont(IPdfStreamWriter writer, IndirectReferenceToken reservedIndirect=null)
        {
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type1  },
                { NameToken.BaseFont, NameToken.Create(metrics.FontName) },
                { NameToken.Encoding, NameToken.MacRomanEncoding }
            };

            var token = new DictionaryToken(dictionary);

            if (reservedIndirect != null)
            {
                return writer.WriteToken(token, reservedIndirect);
            }

            var result = writer.WriteToken(token);

            return result;
        }

        public byte GetValueForCharacter(char character)
        {
            var name = GlyphList.AdobeGlyphList.UnicodeCodePointToName(character);

            if (name == null || !MacRomanEncoding.Instance.NameToCodeMap.TryGetValue(name, out var code))
            {
                var nameError = name ?? "NULL";
                throw new NotSupportedException($"No mapping for character '{character}' exists in the Standard14 font. Glyph name: '{nameError}'.");
            }

            if (code > byte.MaxValue)
            {
                throw new NotSupportedException($"Value of code for character '{character}' exceeded the range of a byte. Glyph name: '{name}'.");
            }

            var result = (byte) code;
            return result;
        }
    }
    
}