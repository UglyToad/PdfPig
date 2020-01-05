namespace UglyToad.PdfPig.Tokenization.Scanner
{
    /// <summary>
    /// The current scope of the <see cref="ITokenScanner"/>.
    /// </summary>
    public enum ScannerScope
    {
        /// <summary>
        /// Reading normally.
        /// </summary>
        None = 0,
        /// <summary>
        /// Reading inside an array.
        /// </summary>
        Array = 1,
        /// <summary>
        /// Reading inside a dictionary.
        /// </summary>
        Dictionary = 2
    }
}