namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using Tokens;

    /// <summary>
    /// Scan input for PostScript/PDF tokens.
    /// </summary>
    public interface ITokenScanner
    {
        /// <summary>
        /// Read the next token in the input.
        /// </summary>
        /// <returns></returns>
        bool MoveNext();

        /// <summary>
        /// The currently read token.
        /// </summary>
        IToken CurrentToken { get; }

        /// <summary>
        /// Try reading a token of the specific type.
        /// </summary>
        bool TryReadToken<T>(out T token) where T : class, IToken;
    }
}