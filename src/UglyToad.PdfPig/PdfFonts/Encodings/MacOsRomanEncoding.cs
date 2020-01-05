namespace UglyToad.PdfPig.PdfFonts.Encodings
{
    using Core;

    /// <summary>
    /// Similar to the <see cref="MacRomanEncoding"/> with 15 additional entries.
    /// </summary>
    internal class MacOsRomanEncoding : MacRomanEncoding
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