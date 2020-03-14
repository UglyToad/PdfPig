namespace UglyToad.PdfPig.Tokens
{
    /// <summary>
    /// A comment from a PDF document. Any occurrence of the percent sign character (%) outside a string or stream
    /// introduces a comment. The comment consists of all characters between the percent sign and the end of the line.
    /// </summary>
    public class CommentToken : IDataToken<string>
    {
        /// <summary>
        /// The text of the comment (excluding the initial percent '%' sign).
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// Create a new <see cref="CommentToken"/>.
        /// </summary>
        /// <param name="data">The text of the comment.</param>
        public CommentToken(string data)
        {
            Data = data ?? string.Empty;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Data;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is CommentToken other))
            {
                return false;
            }

            return other.Data == Data;
        }
    }
}
