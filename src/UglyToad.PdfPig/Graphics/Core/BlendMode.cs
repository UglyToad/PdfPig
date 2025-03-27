namespace UglyToad.PdfPig.Graphics.Core
{
    /// <summary>
    /// The blend mode.
    /// </summary>
    public enum BlendMode : byte
    {
        // 11.3.5.2 Separable blend modes

        /// <summary>
        /// Default.
        /// <para>Same as Compatible.</para>
        /// </summary>
        Normal = 0,
        Multiply = 1,
        Screen = 2,
        Darken = 3,
        Lighten = 4,
        ColorDodge = 5,
        ColorBurn = 6,
        HardLight = 7,
        SoftLight = 8,
        Overlay = 9,
        Difference = 10,
        Exclusion = 11,

        // 11.3.5.3 Non-separable blend modes
        Hue = 12,
        Saturation = 13,
        Color = 14,
        Luminosity = 15
    }

    internal static class BlendModeExtensions
    {
        public static BlendMode? ToBlendMode(this string s)
        {
            return s switch
            {
                // 11.3.5.2 Separable blend modes
                "Normal" => BlendMode.Normal,
                "Compatible" => BlendMode.Normal,
                "Multiply" => BlendMode.Multiply,
                "Screen" => BlendMode.Screen,
                "Darken" => BlendMode.Darken,
                "Lighten" => BlendMode.Lighten,
                "ColorDodge" => BlendMode.ColorDodge,
                "ColorBurn" => BlendMode.ColorBurn,
                "HardLight" => BlendMode.HardLight,
                "SoftLight" => BlendMode.SoftLight,
                "Overlay" => BlendMode.Overlay,
                "Difference" => BlendMode.Difference,
                "Exclusion" => BlendMode.Exclusion,

                // 11.3.5.3 Non-separable blend modes
                "Hue" => BlendMode.Hue,
                "Saturation" => BlendMode.Saturation,
                "Color" => BlendMode.Color,
                "Luminosity" => BlendMode.Luminosity,

                _ => null
            };
        }
    }
}
