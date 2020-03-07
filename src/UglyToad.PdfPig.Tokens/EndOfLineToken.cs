namespace UglyToad.PdfPig.Tokens
{
    /// <summary>
    /// Represents an End Of Line marker found in Adobe Type 1 font files and the cross-reference table.
    /// </summary>
    public class EndOfLineToken : IToken
    {
        /// <summary>
        /// The instance of the end of line token.
        /// </summary>
        public static EndOfLineToken Token { get; } = new EndOfLineToken();

        private EndOfLineToken()
        {
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (this == obj)
                return true;

            if (!(obj is EndOfLineToken other))
            {
                return false;
            }

            return true;
        }
    }
}