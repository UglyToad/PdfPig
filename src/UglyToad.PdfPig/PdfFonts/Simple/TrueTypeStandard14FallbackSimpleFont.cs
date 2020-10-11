namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Fonts;
    using Fonts.AdobeFontMetrics;
    using Fonts.Encodings;
    using Fonts.TrueType;
    using Tokens;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Tokenization.Scanner;

    /// <summary>
    /// Some TrueType fonts use both the Standard 14 descriptor and the TrueType font from disk.
    /// </summary>
    internal class TrueTypeStandard14FallbackSimpleFont : IFont
    {
        private static readonly TransformationMatrix DefaultTransformation =
            TransformationMatrix.FromValues(1 / 1000.0, 0, 0, 1 / 1000.0, 0, 0);

        private readonly AdobeFontMetrics fontMetrics;
        private readonly Encoding encoding;
        private readonly TrueTypeFont font;
        private readonly MetricOverrides overrides;

        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; set; }

        public TrueTypeStandard14FallbackSimpleFont(NameToken name, AdobeFontMetrics fontMetrics, Encoding encoding, TrueTypeFont font,
            MetricOverrides overrides)
        {
            this.fontMetrics = fontMetrics;
            this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            this.font = font;
            this.overrides = overrides;
            Name = name;
            Details = fontMetrics == null ? FontDetails.GetDefault(Name?.Data) : new FontDetails(Name?.Data, null,
                fontMetrics.Weight == "Bold",
                fontMetrics.Weight == "Bold" ? 700 : FontDetails.DefaultWeight,
                fontMetrics.ItalicAngle != 0);
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
            var width = 0.0;

            var fontMatrix = GetFontMatrix();

            if (font != null && font.TryGetBoundingBox(characterCode, out var bounds))
            {
                bounds = fontMatrix.Transform(bounds);

                if (overrides?.TryGetWidth(characterCode, out width) != true)
                {
                    width = bounds.Width;
                }
                else
                {
                    width = DefaultTransformation.TransformX(width);
                }

                return new CharacterBoundingBox(bounds, width);
            }

            var name = encoding.GetName(characterCode);
            if (!fontMetrics.CharacterMetrics.TryGetValue(name, out var metrics))
            {
                return new CharacterBoundingBox(new PdfRectangle(), 0);
            }

            if (overrides?.TryGetWidth(characterCode, out width) != true)
            {
                width = fontMatrix.TransformX(metrics.Width.X);
            }
            else
            {
                width = DefaultTransformation.TransformX(width);
            }

            bounds = fontMatrix.Transform(metrics.BoundingBox);

            return new CharacterBoundingBox(bounds, width);
        }

        public TransformationMatrix GetFontMatrix()
        {
            if (font?.TableRegister.HeaderTable != null)
            {
                var scale = (double)font.GetUnitsPerEm();

                return TransformationMatrix.FromValues(1 / scale, 0, 0, 1 / scale, 0, 0);
            }

            return DefaultTransformation;
        }

        public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path)
        {
            path = new List<PdfSubpath>();
            return false;
        }

        public bool TryGetDecodedFontBytes(IPdfTokenScanner pdfTokenScanner, IFilterProvider filterProvider, out IReadOnlyList<byte> bytes)
        {
            bytes = font?.FontFileBytes;
            return bytes != null;
        }

        public class MetricOverrides
        {
            public int? FirstCharacterCode { get; }

            public IReadOnlyList<double> Widths { get; }

            public bool HasOverriddenMetrics { get; }

            public MetricOverrides(int? firstCharacterCode, IReadOnlyList<double> widths)
            {
                FirstCharacterCode = firstCharacterCode;
                Widths = widths;
                HasOverriddenMetrics = FirstCharacterCode.HasValue && Widths != null
                    && Widths.Count > 0;
            }

            public bool TryGetWidth(int characterCode, out double width)
            {
                width = 0;

                if (!HasOverriddenMetrics || !FirstCharacterCode.HasValue)
                {
                    return false;
                }

                var index = characterCode - FirstCharacterCode.Value;

                if (index < 0 || index >= Widths.Count)
                {
                    return false;
                }

                width = Widths[index];

                return true;
            }
        }
    }
}