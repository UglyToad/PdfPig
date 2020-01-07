namespace UglyToad.PdfPig.Fonts.Encodings
{
    using Core;

    /// <inheritdoc />
    /// <summary>
    /// Similar to the <see cref="T:UglyToad.PdfPig.Fonts.Encodings.MacRomanEncoding" /> with 15 additional entries.
    /// </summary>
    public class MacOsRomanEncoding : MacRomanEncoding
    {
        private static readonly (int, string)[] EncodingTable =
        {
            (255, "notequal"),
            (260, "infinity"),
            (262, "lessequal"),
            (263, "greaterequal"),
            (266, "partialdiff"),
            (267, "summation"),
            (270, "product"),
            (271, "pi"),
            (272, "integral"),
            (275, "Omega"),
            (303, "radical"),
            (305, "approxequal"),
            (306, "Delta"),
            (327, "lozenge"),
            (333, "Euro"),
            (360, "apple")
        };

        /// <summary>
        /// The single instance of this encoding.
        /// </summary>
        public new static MacOsRomanEncoding Instance { get; } = new MacOsRomanEncoding();

        private MacOsRomanEncoding()
        {
            foreach (var valueTuple in EncodingTable)
            {
                Add(OctalHelpers.FromOctalInt(valueTuple.Item1), valueTuple.Item2);
            }
        }
    }
}