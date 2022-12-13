namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Core;
    using PdfPig.Fonts;
    using PdfPig.Fonts.AdobeFontMetrics;
    using PdfPig.Fonts.Encodings;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Colors;
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
            boundingBox = default(PdfRectangle);        
            var characterMetric = metrics.CharacterMetrics
                                   .Where(v => v.Value.CharacterCode == character)
                                   .Select(v => v.Value)
                                   .FirstOrDefault();
            if (characterMetric is null)
            {
                Debug.WriteLine($"Font '{metrics.FontName}' does NOT have character '{character}' (0x{(int)character:X}).");
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
                { NameToken.Encoding, (metrics.FontName is "Symbol" or "ZapfDingbats") ? NameToken.Create("FontSpecific") : NameToken.StandardEncoding }   //  2022-12-12 @fnatzke was NameToken.MacRomanEncoding; not sure based on spec why MacRomanEncoding enoding?
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
           
            var characterMetric = metrics.CharacterMetrics
                                    .Where(v => v.Value.CharacterCode == character)
                                    .Select(v => v.Value)
                                    .FirstOrDefault();
            if (characterMetric is null)
            {
                throw new NotSupportedException($"Font '{metrics.FontName}' does NOT have character '{character}' (0x{(int)character:X}).");
            }
            var code = characterMetric.CharacterCode;
            var result = (byte)code;
            return result;
        } 
    }   
}