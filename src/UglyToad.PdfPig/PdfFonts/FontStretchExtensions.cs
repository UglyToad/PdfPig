namespace UglyToad.PdfPig.PdfFonts
{
    using Tokens;

    internal static class FontStretchExtensions
    {
        public static FontStretch ConvertToFontStretch(this NameToken name)
        {
            return name.Data switch {
                "UltraCondensed" => FontStretch.UltraCondensed,
                "ExtraCondensed" => FontStretch.ExtraCondensed,
                "Condensed"      => FontStretch.Condensed,
                "Normal"         => FontStretch.Normal,
                "SemiExpanded"   => FontStretch.SemiExpanded,
                "Expanded"       => FontStretch.Expanded,
                "ExtraExpanded"  => FontStretch.ExtraExpanded,
                "UltraExpanded"  => FontStretch.UltraExpanded,
                _                => FontStretch.Unknown
            };
        }
    }
}