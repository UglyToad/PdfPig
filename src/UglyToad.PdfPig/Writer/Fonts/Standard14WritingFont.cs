namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Core;
    using PdfPig.Fonts;
    using PdfPig.Fonts.AdobeFontMetrics;
    using PdfPig.Fonts.Encodings;
    using Tokens;

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

            int code = CodeMapIfUnicode(character);
            if (code == -1)
            {
                Debug.WriteLine($"Font '{metrics.FontName}' does NOT have character '{character}' (0x{(int)character:X}).");
                return false;
            }

            var characterMetric = metrics.CharacterMetrics
                                   .Where(v => v.Value.CharacterCode == code)
                                   .Select(v => v.Value)
                                   .FirstOrDefault();
            if (characterMetric == null)
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
            return TransformationMatrix.FromValues(1 / 1000.0, 0, 0, 1 / 1000.0, 0, 0);
        }

        public IndirectReferenceToken WriteFont(IPdfStreamWriter writer, IndirectReferenceToken reservedIndirect = null)
        {
            var encoding = NameToken.StandardEncoding;
            if (string.Equals(metrics.FontName, "Symbol", StringComparison.OrdinalIgnoreCase)
                || string.Equals(metrics.FontName, "ZapfDingbats", StringComparison.OrdinalIgnoreCase))
            {
                //  2022-12-12 @fnatzke was NameToken.MacRomanEncoding; not sure based on spec why MacRomanEncoding encoding?
                encoding = NameToken.Create("FontSpecific");
            }

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type1  },
                { NameToken.BaseFont, NameToken.Create(metrics.FontName) },
                { NameToken.Encoding, encoding }
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
            var characterCode = CodeMapIfUnicode(character);
            var characterMetric = metrics.CharacterMetrics
                                    .Where(v => v.Value.CharacterCode == characterCode)
                                    .Select(v => v.Value)
                                    .FirstOrDefault();
            if (characterMetric == null)
            {
                throw new NotSupportedException($"Font '{metrics.FontName}' does NOT have character '{character}' (0x{(int)character:X}).");
            }
            var code = characterMetric.CharacterCode;
            var result = (byte)code;
            return result;
        }
        private int UnicodeToSymbolCode(char character)
        {
            var name = GlyphList.AdobeGlyphList.UnicodeCodePointToName(character);
            if (string.Equals(name, ".notdef", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }
            var symbolEncoding = SymbolEncoding.Instance;
            var code = symbolEncoding.GetCode(name);
            if (code == -1)
            {
                Debug.WriteLine($"Found Unicode point '{character}' (0x{(int)character:X}) but glphy name '{name}' not found in font '{metrics.FontName}' [Symbol] (StandardEncoding).");
            }
            return code;
        }

        private int UnicodeToZapfDingbats(char character)
        {
            var name = GlyphList.ZapfDingbats.UnicodeCodePointToName(character);
            if (string.Equals(name, ".notdef", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Failed to find Unicode character '{character}' (0x{(int)character:X}).");
                return -1;
            }

            var encoding = ZapfDingbatsEncoding.Instance;
            var code = encoding.GetCode(name);
            if (code == -1)
            {
                Debug.WriteLine($"Found Unicode point '{character}' (0x{(int)character:X}) but glphy name '{name}' not found in font '{metrics.FontName}' (font specific encoding: ZapfDingbats).");
            }
            return code;

        }

        private int UnicodeToStandardEncoding(char character)
        {
            var name = GlyphList.AdobeGlyphList.UnicodeCodePointToName(character);
            if (string.Equals(name, ".notdef", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Failed to find Unicode character '{character}' (0x{(int)character:X}).");
                return -1;
            }
            var standardEncoding = StandardEncoding.Instance;
            var code = standardEncoding.GetCode(name);
            if (code == -1)
            {
                // Check if name from glyph list is the same except first letter's case; capital letter (or if capital a lowercase)
                var nameCapitalisedChange = Char.IsUpper(name[0]) ? Char.ToLower(name[0]) + name.Substring(1) : Char.ToUpper(name[0]) + name.Substring(1);
                code = standardEncoding.GetCode(nameCapitalisedChange);
                if (code == -1)
                {
                    Debug.WriteLine($"Found Unicode point '{character}' (0x{(int)character:X}) but glphy name '{name}' not found in font '{metrics.FontName}' (StandardEncoding).");
                }
            }
            return code;
        }

        private int CodeMapIfUnicode(char character)
        {
            int code; // encoding code either from StandardEncoding, ZapfDingbatsEncoding or SymbolEncoding depending on font.
            int i = character;
            if (string.Equals(metrics.FontName, "ZapfDingbats", StringComparison.OrdinalIgnoreCase))
            {
                // Either use character code as is if font specific code or map from Unicode Dingbats range. 0x2700 - 0x27bf.
                code = i < 255 ? i : UnicodeToZapfDingbats(character);
            }
            else if (string.Equals(metrics.FontName, "Symbol", StringComparison.OrdinalIgnoreCase))
            {
                if (i == 0x00AC)
                {
                    Debug.WriteLine("Warning: 0x00AC used as Unicode ('¬') (logicalnot). For (arrowleft)('←') from Adobe Symbol Font Specific (0330) use Unicode 0x2190 ('←').");
                    return 0x00d8;
                }
                if (i == 0x00F7)
                {
                    Debug.WriteLine("Warning: 0x00F7 used as Unicode ('÷')(divide). For (parenrightex) from Adobe Symbol Font Specific (0367) use Unicode 0xF8F7.");
                    return 0x00B8;
                }
                if (i == 0x00B5)
                {
                    Debug.WriteLine("Warning: 0x00B5 used as Unicode divide ('µ')(mu). For (proportional)('∝')  from Adobe Symbol Font Specific (0265) use Unicode 0x221D('∝').");
                    return 0x006d;
                }
                if (i == 0x00D7)
                {
                    Debug.WriteLine("Warning: 0x00D7 used as Unicode multiply ('×')(multiply). For (dotmath)('⋅')  from Adobe Symbol Font Specific (0327) use Unicode 0x22C5('⋅').");
                    return 0x00B4;
                }

                // Either use character code as is if font specific code or map from Unicode 
                code = i < 255 ? i : UnicodeToSymbolCode(character);
            }
            else
            {
                // Resolve clashes as unicode. Use any standard code (with warnings). Map remainder as unicode to stanard character code.

                if (i == 0x00c6)
                {
                    Debug.WriteLine("Warning: 0x00C6 used as Unicode ('Æ') (AE). For (breve)('˘') from Adobe Standard Font Specific (0306) use Unicode 0x02D8 ('˘').");
                    return 0x00e1; // (341)
                }

                if (i == 0x00b4)
                {
                    Debug.WriteLine("Warning: 0x00B4 used as Unicode ('´') (acute). For (periodcentered)('·') from Adobe Standard Font Specific (0264) use Unicode 0x00B7  ('·').");
                    return 0x00c2; // (0302)
                }



                if (i == 0x00b7)
                {
                    Debug.WriteLine("Warning: 0x00B7 used as Unicode ('·') (periodcentered). For (bullet)('•') from Adobe Standard Font Specific (0267) use Unicode 0x2022 ('•').");
                    return 0x00b4; // (0264)
                }

                if (i == 0x00b8)
                {
                    Debug.WriteLine("Warning: 0x00B8 used as Unicode ('¸') (cedilla). For (quotesinglbase)('‚') from Adobe Standard Font Specific (0267) use Unicode 0x201A ('‚').");
                    return 0x00cb; // (0313)
                }

                if (i == 0x00a4)
                {
                    Debug.WriteLine("Warning: 0x00A4 used as Unicode (currency). For (fraction) ('⁄') from Adobe Standard Font Specific (0244) use Unicode 0x2044 ('⁄').");
                    return 0x00a8; // (0250)
                }

                if (i == 0x00a8)
                {
                    Debug.WriteLine("Warning: 0x00A8 used as Unicode (dieresis)('¨'). For (currency) from Adobe Standard Font Specific (0250) use Unicode 0x00A4.");
                    return 0x00c8; // (0310)
                }

                if (i == 0x0060)
                {
                    Debug.WriteLine("Warning: 0x0060 used as Unicode (grave)('`'). For (quoteleft)('‘') from Adobe Standard Font Specific (0140) use Unicode 0x2018.");
                    return 0x00c1; // (0301)
                }

                if (i == 0x00af)
                {
                    Debug.WriteLine("Warning: 0x00AF used as Unicode (macron)('¯'). For (fl)('ﬂ') from Adobe Standard Font Specific (0257) use Unicode 0xFB02.");
                    return 0x00c5; // (0305)
                }

                if (i == 0x00aa)
                {
                    Debug.WriteLine("Warning: 0x00AA used as Unicode (ordfeminine)('ª'). For (quotedblleft) ('“') from Adobe Standard Font Specific (0252) use Unicode 0x201C.");
                    return 0x00e3; // (0343)
                }

                if (i == 0x00ba)
                {
                    Debug.WriteLine("Warning: 0x00BA used as Unicode (ordmasculine)('º'). For (quotedblright) ('”') from Adobe Standard Font Specific (0272) use Unicode 0x201D.");
                    return 0x00eb; // (0353)
                }

                if (i == 0x00f8)
                {
                    Debug.WriteLine("Warning: 0x00F8 used as Unicode (oslash)('ø'). For (lslash) ('ł') from Adobe Standard Font Specific (0370) use Unicode 0x0142.");
                    return 0x00f9; // (0371)
                }

                if (i == 0x0027)
                {
                    Debug.WriteLine("Warning: 0x0027 used as Unicode (quotesingle)('\''). For (quoteright) ('’') from Adobe Standard Font Specific (0047) use Unicode 0x2019.");
                    return 0x00a9; // (0251)
                }

                var isCharacterCodeInStandardEncoding = metrics.CharacterMetrics.Any(v => v.Value.CharacterCode == i);
                code = isCharacterCodeInStandardEncoding ? i : UnicodeToStandardEncoding(character);
            }
            return code;
        }

    }
}