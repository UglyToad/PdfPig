namespace UglyToad.PdfPig.Tokenization.Scanner
{
    /// <inheritdoc />
    /// <summary>
    /// A <see cref="T:UglyToad.PdfPig.Tokenization.Scanner.ITokenScanner" /> that supports seeking in the underlying input data.
    /// </summary>
    public interface ISeekableTokenScanner : ITokenScanner
    {
        /// <summary>
        /// Move to the specified position.
        /// </summary>
        void Seek(long position);

        /// <summary>
        /// The current position in the input.
        /// </summary>
        long CurrentPosition { get; }

        /// <summary>
        /// The length of the data represented by this scanner.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Add support for a custom type of tokenizer.
        /// </summary>
        void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer);

        /// <summary>
        /// Remove support for a custom type of tokenizer added with <see cref="RegisterCustomTokenizer"/>.
        /// </summary>
        void DeregisterCustomTokenizer(ITokenizer tokenizer);
    }
}