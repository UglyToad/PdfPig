namespace UglyToad.PdfPig.Tokens
{
    using System;

    /// <summary>
    /// Inline image data is used to embed images in PDF content streams. The content is wrapped by ID and ED tags in a BI operation.
    /// </summary>
    public sealed class InlineImageDataToken : IDataToken<ReadOnlyMemory<byte>>
    {
        /// <inheritdoc />
        public ReadOnlyMemory<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="InlineImageDataToken"/>.
        /// </summary>
        /// <param name="data"></param>
        public InlineImageDataToken(ReadOnlyMemory<byte> data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is InlineImageDataToken other))
            {
                return false;
            }

            return Data.Span.SequenceEqual(other.Data.Span);
        }
    }
}