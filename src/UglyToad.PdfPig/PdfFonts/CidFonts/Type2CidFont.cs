namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Core;
    using Geometry;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Type 2 CID fonts contains glyph descriptions based on
    /// the TrueType font format.
    /// </summary>
    internal sealed class Type2CidFont : ICidFont
    {
        private readonly ICidFontProgram? fontProgram;
        private readonly VerticalWritingMetrics verticalWritingMetrics;
        private readonly IReadOnlyDictionary<int, double> widths;
        private readonly double? defaultWidth;
        private readonly CharacterIdentifierToGlyphIndexMap cidToGid;

        public NameToken Type { get; }

        public NameToken SubType { get; }

        public NameToken BaseFont { get; }

        public CharacterIdentifierSystemInfo SystemInfo { get; }

        public TransformationMatrix FontMatrix { get; }

        public CidFontType CidFontType => CidFontType.Type2;

        public FontDescriptor Descriptor { get; }

        public FontDetails Details => fontProgram?.Details ?? Descriptor?.ToDetails(BaseFont?.Data)
            ?? FontDetails.GetDefault(BaseFont?.Data);

        public Type2CidFont(
            NameToken type,
            NameToken subType,
            NameToken baseFont,
            CharacterIdentifierSystemInfo systemInfo,
            FontDescriptor descriptor,
            ICidFontProgram? fontProgram,
            VerticalWritingMetrics verticalWritingMetrics,
            IReadOnlyDictionary<int, double> widths,
            double? defaultWidth,
            CharacterIdentifierToGlyphIndexMap cidToGid)
        {
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            Descriptor = descriptor;
            this.fontProgram = fontProgram;
            this.verticalWritingMetrics = verticalWritingMetrics;
            this.widths = widths;
            this.defaultWidth = defaultWidth;
            this.cidToGid = cidToGid;

            var scale = 1 / (double)(fontProgram?.GetFontMatrixMultiplier() ?? 1000);
            FontMatrix = TransformationMatrix.FromValues(scale, 0, 0, scale, 0, 0);

            // NB: For the font matrixPdfBox always return 1/1000 with the comment '1000 upem, this is not strictly true'
            // see https://github.com/apache/pdfbox/blob/a5379f5588ee4c98222ee61366ad3d82e0f2264e/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/font/PDCIDFontType2.java#L191
            // Always using 1/1000 breaks the 'ReadWordsFromOldGutnishPage1' test
        }

        public double GetWidthFromFont(int characterIdentifier)
        {
            if (fontProgram is null)
            {
                return GetWidthFromDictionary(characterIdentifier);
            }

            if (fontProgram.TryGetBoundingAdvancedWidth(characterIdentifier, cidToGid.GetGlyphIndex, out var width))
            {
                return width;
            }

            // TODO: Read the font width from the font program.
            return GetWidthFromDictionary(characterIdentifier);
        }

        public double GetWidthFromDictionary(int characterIdentifier)
        {
            if (widths.TryGetValue(characterIdentifier, out var width))
            {
                return width;
            }

            if (defaultWidth.HasValue)
            {
                return defaultWidth.Value;
            }

            return Descriptor?.MissingWidth ?? 1000;
        }

        public PdfRectangle GetBoundingBox(int characterIdentifier)
        {
            if (fontProgram is null)
            {
                return Descriptor.BoundingBox;
            }

            if (fontProgram.TryGetBoundingBox(characterIdentifier, cidToGid.GetGlyphIndex, out var result))
            {
                return result;
            }

            return Descriptor.BoundingBox;
        }

        public PdfVector GetPositionVector(int characterIdentifier)
        {
            var width = GetWidthFromFont(characterIdentifier);

            return verticalWritingMetrics.GetPositionVector(characterIdentifier, width);
        }

        public PdfVector GetDisplacementVector(int characterIdentifier)
        {
            return verticalWritingMetrics.GetDisplacementVector(characterIdentifier);
        }

        public TransformationMatrix GetFontMatrix(int characterIdentifier)
        {
            return FontMatrix;
        }

        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path) => TryGetPath(characterCode, cidToGid.GetGlyphIndex, out path);

        public bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;
            if (fontProgram is null)
            {
                return false;
            }

            return fontProgram.TryGetPath(characterCode, characterCodeToGlyphId, out path);
        }

        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            return TryGetNormalisedPath(characterCode, cidToGid.GetGlyphIndex, out path);
        }

        public bool TryGetNormalisedPath(int characterCode, Func<int, int?> characterCodeToGlyphId, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;
            if (fontProgram is null)
            {
                return false;
            }

            if (fontProgram.TryGetPath(characterCode, characterCodeToGlyphId, out path))
            {
                path = GetFontMatrix(characterCode).Transform(path).ToArray();
                return true;
            }

            return false;
        }
    }
}