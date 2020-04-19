namespace UglyToad.PdfPig.Util
{
    using Content;

    /// <summary>
    /// Measures of whitespace size based on point size.
    /// </summary>
    public static class WhitespaceSizeStatistics
    {
        /// <summary>
        /// Get the average whitespace sized expected for a given letter.
        /// </summary>
        public static double GetExpectedWhitespaceSize(Letter letter) => letter.PointSize * 0.27;

        /// <summary>
        /// Check if the measured gap is probably big enough to be a whitespace character based on the letter.
        /// </summary>
        public static bool IsProbablyWhitespace(double gap, Letter letter) => gap > (GetExpectedWhitespaceSize(letter) - (letter.PointSize * 0.05));
    }
}
