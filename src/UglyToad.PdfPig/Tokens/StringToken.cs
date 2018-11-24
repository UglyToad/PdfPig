namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Represents a string of text contained in a PDF document.
    /// </summary>
    public class StringToken : IDataToken<string>
    {
        /// <summary>
        /// The string in the token.
        /// </summary>
        [NotNull]
        public string Data { get; }

        /// <summary>
        /// Create a new <see cref="StringToken"/>.
        /// </summary>
        /// <param name="data">The string data for the token to contain.</param>
        public StringToken([NotNull] string data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"({Data})";
        }
    }
}