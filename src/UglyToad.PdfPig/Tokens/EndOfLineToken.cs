namespace UglyToad.PdfPig.Tokens
{
    internal class EndOfLineToken : IToken
    {
        public static EndOfLineToken Token { get; } = new EndOfLineToken();

        private EndOfLineToken()
        {
        }
    }
}