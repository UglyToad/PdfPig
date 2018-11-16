namespace UglyToad.PdfPig.Tokens
{
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A comment from a PDF document. Any occurrence of the percent sign character (%) outside a string or stream
    /// introduces a comment. The comment consists of all characters between the percent sign and the end of the line.
    /// </summary>
    internal class CommentToken : IDataToken<string>
    {
        /// <summary>
        /// The text of the comment (excluding the initial percent '%' sign).
        /// </summary>
        [CanBeNull]
        public string Data { get; }

        /// <summary>
        /// Create a new <see cref="CommentToken"/>.
        /// </summary>
        /// <param name="data">The text of the comment.</param>
        public CommentToken([CanBeNull]string data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return Data ?? "NULL";
        }
    }
}
