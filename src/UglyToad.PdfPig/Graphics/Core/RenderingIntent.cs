namespace UglyToad.PdfPig.Graphics.Core
{
    /// <summary>
    /// Specifying a rendering intent allows a PDF file to set priorities regarding which properties to preserve and which to sacrifice for CIE colors. 
    /// </summary>
    public enum RenderingIntent
    {
        /// <summary>
        /// No correction for the output medium's white point. Colors 
        /// only represented relative to the light source.
        /// </summary>
        AbsoluteColorimetric = 0,
        /// <summary>
        /// Combines light source and output medium's white point.
        /// </summary>
        RelativeColorimetric = 1,
        /// <summary>
        /// Emphasises saturation rather than colorimetric accuracy.
        /// </summary>
        Saturation = 2,
        /// <summary>
        /// Modifies from colorimetric values to provide a "pleasing perceptual appearance".
        /// </summary>
        Perceptual = 3
    }

    internal static class RenderingIntentExtensions
    {
        public static RenderingIntent ToRenderingIntent(this string s)
        {
            return s switch {
                "AbsoluteColorimetric" => RenderingIntent.AbsoluteColorimetric,
                "RelativeColorimetric" => RenderingIntent.RelativeColorimetric,
                "Saturation"           => RenderingIntent.Saturation,
                "Perceptual"           => RenderingIntent.Perceptual,

                // If the application does not recognise the name it uses RelativeColorimetric by default.
                _ => RenderingIntent.RelativeColorimetric
            };
        }
    }
}
