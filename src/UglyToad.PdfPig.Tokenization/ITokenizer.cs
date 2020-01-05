namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using Tokens;

    /// <summary>
    /// Reads tokens from input data.
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Whether this tokenizer type reads the byte following the token itself to detect if the token has ended.
        /// </summary>
        bool ReadsNextByte { get; }

        /// <summary>
        /// Try and read the token of the corresponding type from the input.
        /// </summary>
        /// <param name="currentByte">The byte read to detect this is the correct tokenizer to use.</param>
        /// <param name="inputBytes">The input data.</param>
        /// <param name="token">The token of the corresponding type if read.</param>
        bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token);
    }
}