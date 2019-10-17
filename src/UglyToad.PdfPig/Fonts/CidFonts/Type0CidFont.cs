namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Geometry;
    using Tokens;

    /// <inheritdoc/>
    /// <summary>
    /// Type 0 CID fonts contain glyph descriptions based on the
    /// Adobe Type 1 font format.
    /// </summary>
    internal class Type0CidFont : ICidFont
    {
        private readonly ICidFontProgram fontProgram;
        private readonly VerticalWritingMetrics verticalWritingMetrics;

        public NameToken Type { get; }

        public NameToken SubType { get; }

        public NameToken BaseFont { get; }

        public CharacterIdentifierSystemInfo SystemInfo { get; }

        public TransformationMatrix FontMatrix { get; }

        public CidFontType CidFontType => CidFontType.Type0;

        public FontDescriptor Descriptor { get; }

        public IReadOnlyDictionary<int, decimal> Widths { get; }

        public Type0CidFont(ICidFontProgram fontProgram, NameToken type, NameToken subType, NameToken baseFont,
            CharacterIdentifierSystemInfo systemInfo,
            FontDescriptor descriptor, VerticalWritingMetrics verticalWritingMetrics, IReadOnlyDictionary<int, decimal> widths)
        {
            this.fontProgram = fontProgram;
            this.verticalWritingMetrics = verticalWritingMetrics;
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            var scale = 1 / (decimal)(fontProgram?.GetFontMatrixMultiplier() ?? 1000);
            FontMatrix = TransformationMatrix.FromValues(scale, 0, 0, scale, 0, 0);
            Descriptor = descriptor;
            Widths = widths;
        }

        public decimal GetWidthFromFont(int characterCode)
        {
            return GetWidthFromDictionary(characterCode);
        }

        public decimal GetWidthFromDictionary(int cid)
        {
            if (cid < 0)
            {
                throw new ArgumentException($"The provided character code was negative: {cid}.");
            }

            if (cid < Widths.Count)
            {
                return Widths[cid];
            }

            // TODO: correct values
            if (Descriptor == null)
            {
                return 250;
            }

            return Descriptor.MissingWidth;
        }

        public PdfRectangle GetBoundingBox(int characterIdentifier)
        {
            // TODO: correct values
            if (characterIdentifier < 0)
            {
                throw new ArgumentException($"The provided character identifier was negative: {characterIdentifier}.");
            }

            if (characterIdentifier < Widths.Count)
            {
                return new PdfRectangle(0, 0, Widths[characterIdentifier], 0);
            }
            
            if (fontProgram.TryGetBoundingBox(characterIdentifier, out var boundingBox))
            {
                return boundingBox;
            }

            return new PdfRectangle(0, 0, 250, 0);
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
    }
}
