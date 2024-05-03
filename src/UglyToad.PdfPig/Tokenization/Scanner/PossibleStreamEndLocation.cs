namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using Tokens;

    /// <summary>
    /// Used internally by the <see cref="PdfTokenScanner"/> when reading streams to store any occurrences of 'endobj' or 'endstream' observed.
    /// </summary>
    internal readonly struct PossibleStreamEndLocation
    {
        /// <summary>
        /// The offset at which the token started in the file.
        /// </summary>
        public long Offset { get; }

        /// <summary>
        /// The type, one of either <see cref="OperatorToken.EndObject"/> or <see cref="OperatorToken.EndStream"/>.
        /// </summary>
        public OperatorToken Type { get; }

        /// <summary>
        /// Create a new <see cref="PossibleStreamEndLocation"/>
        /// </summary>
        public PossibleStreamEndLocation(long offset, OperatorToken type)
        {
            Offset = offset;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public override string ToString()
        {
            return $"{Offset}: {Type}";
        }
    }
}