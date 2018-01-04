namespace UglyToad.Pdf.Tokenization
{
    using IO;
    using Tokens;

    internal class CrossReferenceEndOfLineTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;
            if (currentByte != '\r' && currentByte != '\n')
            {
                return false;
            }

            token = EndOfLineToken.Token;

            return true;
        }
    }

    internal class EndOfLineToken : IToken
    {
        public static EndOfLineToken Token { get; } = new EndOfLineToken();

        private EndOfLineToken()
        {
        }
    }
}
