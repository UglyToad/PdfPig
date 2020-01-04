namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    /// <summary>
    /// A ligature in an Adobe Font Metrics individual character.
    /// </summary>
    public class AdobeFontMetricsLigature
    {
        /// <summary>
        /// The character to join with to form a ligature.
        /// </summary>
        public string Successor { get; }

        /// <summary>
        /// The current character.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Create a new <see cref="AdobeFontMetricsLigature"/>.
        /// </summary>
        public AdobeFontMetricsLigature(string successor, string value)
        {
            Successor = successor;
            Value = value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Ligature: {Value} -> Successor: {Successor}";
        }
    }
}
