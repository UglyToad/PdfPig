namespace UglyToad.PdfPig.Fonts.Encodings
{
    /// <inheritdoc />
    /// <summary>
    /// Similar to the <see cref="T:UglyToad.PdfPig.Fonts.Encodings.MacRomanEncoding" /> with 15 additional entries.
    /// </summary>
    public sealed class MacOsRomanEncoding : MacRomanEncoding
    {
        private static readonly (int, string)[] EncodingTable =
        {
            (0255, "notequal"),
            (0260, "infinity"),
            (0262, "lessequal"),
            (0263, "greaterequal"),
            (0266, "partialdiff"),
            (0267, "summation"),
            (0270, "product"),
            (0271, "pi"),
            (0272, "integral"),
            (0275, "Omega"),
            (0303, "radical"),
            (0305, "approxequal"),
            (0306, "Delta"),
            (0327, "lozenge"),
            (0333, "Euro"),
            (0360, "apple")
        };

        /// <summary>
        /// The single instance of this encoding.
        /// </summary>
        public new static MacOsRomanEncoding Instance { get; } = new MacOsRomanEncoding();

        private MacOsRomanEncoding()
        {
            foreach ((var codeToBeConverted, var name) in EncodingTable)
            {
                // In source code an int literal with a leading zero ('0')
                // in other languages ('C' and 'Java') would be interpreted
                // as octal (base 8) and converted but C# does not support and
                // so arrives here as a different value parsed as base10.
                // Convert 'codeToBeConverted' to intended value as if it was an octal literal before using.
                // For example 040 converts to string "40" then convert string to int again but using base 8 (octal) so result is 32 (base 10).
                var code = System.Convert.ToInt32($"{codeToBeConverted}", 8);  // alternative is OctalHelpers.FromOctalInt()
                Add(code, name);
            }
        }
    }
}