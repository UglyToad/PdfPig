namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using Core;
    using Tokens;

    /// <summary>
    /// Tokenizes objects from bytes in a PDF file.
    /// </summary>
    public interface IPdfTokenScanner : ISeekableTokenScanner, IDisposable
    {
        /// <summary>
        /// Tokenize the object with a given object number.
        /// May return null when the reference is undefined
        /// </summary>
        /// <param name="reference">The object number for the object to tokenize.</param>
        /// <returns>The tokenized object.</returns>
        ObjectToken Get(IndirectReference reference);

        /// <summary>
        /// Adds the token to an internal cache that will be returned instead of
        /// scanning the source PDF data.
        /// </summary>
        /// <param name="reference">The object number for the object to replace.</param>
        /// <param name="token">The token to replace the existing data.</param>
        void ReplaceToken(IndirectReference reference, IToken token);
    }
}