namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using Tokens;

    internal class EndOfLineTokenizer : ITokenizer
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
}
