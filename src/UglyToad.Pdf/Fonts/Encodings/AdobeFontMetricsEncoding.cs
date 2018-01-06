namespace UglyToad.Pdf.Fonts.Encodings
{
    using System;

    internal class AdobeFontMetricsEncoding : Encoding
    {
        public override string EncodingName { get; } = "AFM";

        public AdobeFontMetricsEncoding(FontMetrics metrics)
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
