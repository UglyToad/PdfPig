namespace UglyToad.PdfPig.PdfFonts.CidFonts
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
        private readonly double? defaultWidth;

        public NameToken Type { get; }

        public NameToken SubType { get; }

        public NameToken BaseFont { get; }

        public CharacterIdentifierSystemInfo SystemInfo { get; }

        public FontDetails Details { get; }

        public TransformationMatrix FontMatrix { get; }

        public CidFontType CidFontType => CidFontType.Type0;

        public FontDescriptor Descriptor { get; }

        public IReadOnlyDictionary<int, double> Widths { get; }

        public Type0CidFont(ICidFontProgram fontProgram, NameToken type, NameToken subType, NameToken baseFont,
            CharacterIdentifierSystemInfo systemInfo,
            FontDescriptor descriptor,
            VerticalWritingMetrics verticalWritingMetrics,
            IReadOnlyDictionary<int, double> widths,
            double? defaultWidth)
        {
            this.fontProgram = fontProgram;
            this.verticalWritingMetrics = verticalWritingMetrics;
            this.defaultWidth = defaultWidth;
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            var scale = 1 / (double)(fontProgram?.GetFontMatrixMultiplier() ?? 1000);
            FontMatrix = TransformationMatrix.FromValues(scale, 0, 0, scale, 0, 0);
            Descriptor = descriptor;
            Widths = widths;

            if (fontProgram == null && Descriptor == null)
            {
                Details = FontDetails.GetDefault(BaseFont?.Data);
            }
            else
            {
                // warning:
                // fontProgram.Details.Weight can be different from Descriptor.ToDetails().Weight
                string fontName = !string.IsNullOrEmpty(fontProgram?.Details?.Name) ? fontProgram?.Details?.Name : (!string.IsNullOrEmpty(Descriptor?.FontName) ? Descriptor?.FontName : BaseFont?.Data);
                string fontFamilly = !string.IsNullOrEmpty(fontProgram?.Details?.FontFamily) ? fontProgram?.Details?.FontFamily : Descriptor?.FontFamily;
                bool isBold = fontProgram?.Details?.IsBold ?? Descriptor?.ToDetails().IsBold ?? false;
                bool isItalic = fontProgram?.Details?.IsItalic ?? Descriptor?.ToDetails().IsItalic ?? false;
                var weight = fontProgram?.Details?.Weight ?? Descriptor?.ToDetails().Weight ?? FontDetails.DefaultWeight;
                Details = new FontDetails(fontName, fontFamilly, isBold, weight, isItalic);
            }
        }

        public double GetWidthFromFont(int characterCode)
        {
            return GetWidthFromDictionary(characterCode);
        }

        public double GetWidthFromDictionary(int cid)
        {
            if (cid < 0)
            {
                throw new ArgumentException($"The provided character code was negative: {cid}.");
            }

            if (Widths.TryGetValue(cid, out var width))
            {
                return width;
            }

            if (defaultWidth.HasValue)
            {
                return defaultWidth.Value;
            }
            
            if (Descriptor == null)
            {
                return 1000;
            }

            return (double)Descriptor.MissingWidth;
        }

        public PdfRectangle GetBoundingBox(int characterIdentifier)
        {
            // TODO: correct values
            if (characterIdentifier < 0)
            {
                throw new ArgumentException($"The provided character identifier was negative: {characterIdentifier}.");
            }

            if (fontProgram == null)
            {
                return Descriptor?.BoundingBox ?? new PdfRectangle(0, 0, 1000, 0);
            }
            
            if (fontProgram.TryGetBoundingBox(characterIdentifier, out var boundingBox))
            {
                return boundingBox;
            }

            if (Widths.TryGetValue(characterIdentifier, out var width))
            {
                return new PdfRectangle(0, 0, width, 0);
            }

            if (defaultWidth.HasValue)
            {
                return new PdfRectangle(0, 0, defaultWidth.Value, 0);
            }

            return new PdfRectangle(0, 0, 1000, 0);
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
