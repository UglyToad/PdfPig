namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using System.Collections.Generic;
    using Core;
    using Geometry;
    using Tokenization.Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Type 2 CID fonts contains glyph descriptions based on
    /// the TrueType font format.
    /// </summary>
    internal class Type2CidFont : ICidFont
    {
        private readonly ICidFontProgram fontProgram;
        private readonly VerticalWritingMetrics verticalWritingMetrics;
        private readonly IReadOnlyDictionary<int, decimal> widths;

        public NameToken Type { get; }
        public NameToken SubType { get; }
        public NameToken BaseFont { get; }
        public CharacterIdentifierSystemInfo SystemInfo { get; }
        public TransformationMatrix FontMatrix { get; }
        public CidFontType CidFontType => CidFontType.Type2;
        public FontDescriptor Descriptor { get; }

        public Type2CidFont(NameToken type, NameToken subType, NameToken baseFont, CharacterIdentifierSystemInfo systemInfo, 
            FontDescriptor descriptor, ICidFontProgram fontProgram,
            VerticalWritingMetrics verticalWritingMetrics,
            IReadOnlyDictionary<int, decimal> widths)
        {
            Type = type;
            SubType = subType;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            Descriptor = descriptor;
            this.fontProgram = fontProgram;
            this.verticalWritingMetrics = verticalWritingMetrics;
            this.widths = widths;

            // TODO: This should maybe take units per em into account?
            var scale = 1 / 1000m;
            FontMatrix = TransformationMatrix.FromValues(scale, 0, 0, scale, 0, 0);
        }

        public decimal GetWidthFromFont(int characterCode)
        {
            // TODO: Read the font width from the font program.
            throw new System.NotImplementedException();
        }

        public decimal GetWidthFromDictionary(int cid)
        {
            if (widths.TryGetValue(cid, out var width))
            {
                return width;
            }

            return Descriptor.MissingWidth;
        }

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            if (fontProgram == null)
            {
                return Descriptor.BoundingBox;
            }

            if (fontProgram.TryGetBoundingBox(characterCode, out var result))
            {
                return result;
            }

            return Descriptor.BoundingBox;
        }
    }
}