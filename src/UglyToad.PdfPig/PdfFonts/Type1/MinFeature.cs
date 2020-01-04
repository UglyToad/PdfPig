namespace UglyToad.PdfPig.PdfFonts.Type1
{
    /// <summary>
    /// Represents the <see cref="Type1PrivateDictionary"/> MinFeature entry which is required for compatibility
    /// and must have the value 16, 16.
    /// </summary>
    internal class MinFeature
    {
        /// <summary>
        /// The first value.
        /// </summary>
        public int First { get; }

        /// <summary>
        /// The second value.
        /// </summary>
        public int Second { get; }

        /// <summary>
        /// The required default value of <see cref="MinFeature"/>.
        /// </summary>
        public static MinFeature Default { get; } = new MinFeature(16, 16);

        /// <summary>
        /// Creates a <see cref="MinFeature"/> array.
        /// </summary>
        /// <param name="first">The first value.</param>
        /// <param name="second">The second value.</param>
        public MinFeature(int first, int second)
        {
            First = first;
            Second = second;
        }

        public override string ToString()
        {
            return $"{{{First} {Second}}}";
        }
    }
}