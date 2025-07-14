//// ReSharper disable CompareOfFloatsByEqualityOperator
namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Fonts;
    using Fonts.AdobeFontMetrics;
    using Fonts.Encodings;
    using Tokens;

    /// <summary>
    /// A font using one of the Adobe Standard 14 fonts. Can use a custom encoding.
    /// </summary>
    internal sealed class Type1Standard14Font : IFont
    {
        private readonly AdobeFontMetrics standardFontMetrics;
        private readonly Encoding encoding;
        private readonly bool isZapfDingbats;

        public NameToken Name { get; }

        public bool IsVertical { get; }

        public FontDetails Details { get; }

        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001, 0, 0, 0.001, 0, 0);

        public Type1Standard14Font(AdobeFontMetrics standardFontMetrics, Encoding? overrideEncoding = null)
        {
            this.standardFontMetrics = standardFontMetrics ?? throw new ArgumentNullException(nameof(standardFontMetrics));
            encoding = overrideEncoding ?? new AdobeFontMetricsEncoding(standardFontMetrics);

            Name = NameToken.Create(standardFontMetrics.FontName);

            IsVertical = false;
            Details = new FontDetails(Name.Data,
                standardFontMetrics.Weight == "Bold",
                standardFontMetrics.Weight == "Bold" ? 700 : FontDetails.DefaultWeight,
                standardFontMetrics.ItalicAngle != 0);
            isZapfDingbats = encoding is ZapfDingbatsEncoding || Details.Name.Contains("ZapfDingbats");
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            value = null;

            var name = encoding.GetName(characterCode);

            if (string.Equals(name, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                if (isZapfDingbats)
                {
                    value = GlyphList.ZapfDingbats.NameToUnicode(name);

                    if (value is not null)
                    {
                        return true;
                    }
                }

                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return value is not null;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            var (boundingBox, advanceWidth) = GetBoundingBoxInGlyphSpace(characterCode);

            boundingBox = fontMatrix.Transform(boundingBox);
            advanceWidth = fontMatrix.TransformX(advanceWidth);

            return new CharacterBoundingBox(boundingBox, advanceWidth);
        }

        private (PdfRectangle bounds, double advanceWidth) GetBoundingBoxInGlyphSpace(int characterCode)
        {
            var name = encoding.GetName(characterCode);

            if (!standardFontMetrics.CharacterMetrics.TryGetValue(name, out var metrics))
            {
                return (new PdfRectangle(0, 0, 250, 0), 250);
            }

            var x = metrics.Width.X;

            if (metrics.Width.X == 0 && metrics.BoundingBox.Width > 0)
            {
                x = metrics.BoundingBox.Width;
            }

            return (metrics.BoundingBox, x);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Not implemented.</para>
        /// </summary>
        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            // https://github.com/apache/pdfbox/blob/trunk/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/Standard14Fonts.java
            path = null;
            return false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Not implemeted.</para>
        /// </summary>
        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            return TryGetPath(characterCode, out path);
        }
    }
}
