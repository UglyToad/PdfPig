namespace UglyToad.Pdf.Fonts.CidFonts
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using Cos;

    internal class CharacterIdentifierFontBuilder
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
}