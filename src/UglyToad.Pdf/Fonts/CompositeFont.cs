namespace UglyToad.Pdf.Fonts
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Cos;

    //public class CompositeFont
    //{
    //    public bool IsSimple { get; } = false;

    //    public CosName SubType { get; } = CosName.TYPE0;

    //    public CharacterIdentifierFont Descendant { get; }
    //}

    /// <summary>
    /// Equivalent to the DW2 array in the font dictionary for vertical fonts.
    /// </summary>
    public struct VerticalVectorComponents
    {
        public decimal Position { get; }

        public decimal Displacement { get; }

        public VerticalVectorComponents(decimal position, decimal displacement)
        {
            Position = position;
            Displacement = displacement;
        }

        public static VerticalVectorComponents Default = new VerticalVectorComponents(800, -1000);
    }

    public enum CidFontType
    {
        /// <summary>
        /// Glyph descriptions based on Adobe Type 1 format.
        /// </summary>
        Type0 = 0,
        /// <summary>
        /// Glyph descriptions based on TrueType format.
        /// </summary>
        Type2 = 2
    }

    public class CharacterIdentifierFont
    {
        public const int DefaultWidthWhenUndeclared = 1000;

        public CidFontType Subtype { get; }

        public CosName BaseFont { get; }

        public CharacterIdentifierSystemInfo SystemInfo { get; set; }

        public CosObjectKey FontDescriptor { get; set; }

        public int DefaultWidth { get; }

        public COSArray Widths { get; set; }

        public VerticalVectorComponents VerticalVectors { get; } = VerticalVectorComponents.Default;

        public CharacterIdentifierToGlyphIdentifierMap CidToGidMap { get; }

        public CharacterIdentifierFont(CidFontType subtype, CosName baseFont, CharacterIdentifierSystemInfo systemInfo,
            CosObjectKey fontDescriptor,
            int defaultWidth,
            COSArray widths,
            CharacterIdentifierToGlyphIdentifierMap cidToGidMap)
        {
            Subtype = subtype;
            BaseFont = baseFont;
            SystemInfo = systemInfo;
            FontDescriptor = fontDescriptor;
            DefaultWidth = defaultWidth;
            Widths = widths;
            CidToGidMap = cidToGidMap;
        }


    }

    public class CharacterIdentifierFontBuilder
    {
        private static readonly IReadOnlyDictionary<CosName, CidFontType> NameTypeMap = new Dictionary<CosName, CidFontType>
        {
            { CosName.CID_FONT_TYPE0, CidFontType.Type0 },
            { CosName.CID_FONT_TYPE2, CidFontType.Type2 }
        };

        private readonly CidFontType subType;
        private readonly CosName baseFont;
        private int defaultWidth = CharacterIdentifierFont.DefaultWidthWhenUndeclared;
        private readonly CharacterIdentifierSystemInfo systemInfo;
        private readonly CosObjectKey fontDescriptorKey;

        public CharacterIdentifierFontBuilder(CosName subType, CosName baseFont,
            CharacterIdentifierSystemInfo systemInfo,
            CosObjectKey fontDescriptorKey)
        {
            if (!NameTypeMap.TryGetValue(subType, out var subTypeValue))
            {
                throw new InvalidOperationException("The subType of the CIDFont was not valid: " + subType);
            }

            this.subType = subTypeValue;
            this.baseFont = baseFont;
            this.systemInfo = systemInfo;
            this.fontDescriptorKey = fontDescriptorKey;
        }

        public void WithDefaultWidth(int width)
        {
            defaultWidth = width;
        }

        public CharacterIdentifierFont Build()
        {
            return new CharacterIdentifierFont(subType, baseFont, systemInfo, fontDescriptorKey, defaultWidth, null, null);
        }
    }

    public class CharacterIdentifierToGlyphIdentifierMap
    {

    }
}
