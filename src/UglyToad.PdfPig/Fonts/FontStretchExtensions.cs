namespace UglyToad.PdfPig.Fonts
{
    using Cos;

    internal static class FontStretchExtensions
    {
        public static FontStretch ConvertToFontStretch(this CosName name)
        {
            switch (name.Name)
            {
                case "UltraCondensed":
                    return FontStretch.UltraCondensed;
                case "ExtraCondensed":
                    return FontStretch.ExtraCondensed;
                case "Condensed":
                    return FontStretch.Condensed;
                case "Normal":
                    return FontStretch.Normal;
                case "SemiExpanded":
                    return FontStretch.SemiExpanded;
                case "Expanded":
                    return FontStretch.Expanded;
                case "ExtraExpanded":
                    return FontStretch.ExtraExpanded;
                case "UltraExpanded":
                    return FontStretch.UltraExpanded;
                default:
                    return FontStretch.Unknown;
            } 
        }
    }
}