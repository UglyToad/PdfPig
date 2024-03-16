﻿//// ReSharper disable CompareOfFloatsByEqualityOperator
namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            var name = encoding.GetName(characterCode);
            if (string.Equals(name, GlyphList.NotDefined, StringComparison.OrdinalIgnoreCase))
            {
                value = null;
                return false;
            }

            if (encoding is ZapfDingbatsEncoding)
            {
                var listed = GlyphList.ZapfDingbats.NameToUnicode(name);

                value = listed;

                return true;
            }

            if (encoding is StandardEncoding || encoding is SymbolEncoding)
            {
                var listed = GlyphList.AdobeGlyphList.NameToUnicode(name);

                value = listed;

                return true;
            }
            else
            {
                Debug.WriteLine($"Warning: Type1Standard14Font with unexpected encoding: '{encoding.EncodingName}' Expected: 'ZapfDingbatsEncoding','SymbolEncoding' or 'StandardEncoding' . Font: '{standardFontMetrics.FontName}'");
                var listed = GlyphList.AdobeGlyphList.NameToUnicode(name);

                value = listed;

                return true;
            }
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            boundingBox = fontMatrix.Transform(boundingBox);

            return new CharacterBoundingBox(boundingBox, boundingBox.Width);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            var name = encoding.GetName(characterCode);

            if (!standardFontMetrics.CharacterMetrics.TryGetValue(name, out var metrics))
            {
                return new PdfRectangle(0, 0, 250, 0);
            }

            var x = metrics.Width.X;
            var y = metrics.Width.Y;

            if (metrics.Width.X == 0 && metrics.BoundingBox.Width > 0)
            {
                x = metrics.BoundingBox.Width;
            }

            if (metrics.Width.Y == 0 && metrics.BoundingBox.Height > 0)
            {
                y = metrics.BoundingBox.Height;
            }

            return new PdfRectangle(0, 0, x, y);
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
