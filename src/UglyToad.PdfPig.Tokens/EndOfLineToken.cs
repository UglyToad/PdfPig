namespace UglyToad.PdfPig.Tokens
{
    /// <summary>
    /// Represents an End Of Line marker found in Adobe Type 1 font files. This is not used by the main PDF tokenizer.
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
    }
}