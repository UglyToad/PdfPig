namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using Tokens;

    /// <summary>
    /// Read an <see cref="EndOfLineToken"/>.
    /// </summary>
    public sealed class EndOfLineTokenizer : ITokenizer
    {
        /// <inheritdoc />
        public bool ReadsNextByte { get; } = false;

        /// <inheritdoc />
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
