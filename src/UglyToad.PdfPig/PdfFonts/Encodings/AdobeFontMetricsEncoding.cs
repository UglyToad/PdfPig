namespace UglyToad.PdfPig.PdfFonts.Encodings
{
    using System;
    using Fonts.AdobeFontMetrics;

    internal class AdobeFontMetricsEncoding : Encoding
    {
        public override string EncodingName { get; } = "AFM";

        public AdobeFontMetricsEncoding(AdobeFontMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            foreach (var characterMetric in metrics.CharacterMetrics)
            {
                Add(characterMetric.Value.CharacterCode, characterMetric.Key);
            }
        }
    }
}
