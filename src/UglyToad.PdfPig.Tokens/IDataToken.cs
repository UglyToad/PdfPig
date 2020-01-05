namespace UglyToad.PdfPig.Tokens
{
    /// <inheritdoc />
    /// <summary>
    /// A token from a PDF document which contains data in some format.
    /// </summary>
    /// <typeparam name="T">The type of the data this token contains.</typeparam>
    public interface IDataToken<out T> : IToken
    {
        /// <summary>
        /// The data this token contains.
        /// </summary>
        T Data { get; }
    }
}