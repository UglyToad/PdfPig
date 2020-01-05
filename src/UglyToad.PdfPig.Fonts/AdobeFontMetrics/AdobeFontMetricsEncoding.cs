namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using System;
    using Encodings;

    /// <inheritdoc />
    /// <summary>
    /// An <see cref="T:UglyToad.PdfPig.Fonts.Encodings.Encoding" /> from an Adobe Font Metrics file.
    /// </summary>
    public class AdobeFontMetricsEncoding : Encoding
    {
        /// <inheritdoc />
        public override string EncodingName { get; } = "AFM";

        /// <summary>
        /// Create a new <see cref="AdobeFontMetricsEncoding"/>.
        /// </summary>
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
