namespace UglyToad.PdfPig.Tokens
{
    using System;

    /// <summary>
    /// Inline image data is used to embed images in PDF content streams. The content is wrapped by ID and ED tags in a BI operation.
    /// </summary>
    public sealed class InlineImageDataToken : IDataToken<Memory<byte>>
    {
        /// <inheritdoc />
        public Memory<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="InlineImageDataToken"/>.
        /// </summary>
        /// <param name="data"></param>
        public InlineImageDataToken(Memory<byte> data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = new HashCode();
#if NET6_0_OR_GREATER
            hash.AddBytes(Data.Span);
#else
            var span = Data.Span;
            for (var i = 0; i < span.Length; i++)
            {
                hash.Add(span[i]);
            }
#endif
            return hash.ToHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not InlineImageDataToken other)
            {
                return false;
            }

            return Data.Span.SequenceEqual(other.Data.Span);
        }
    }
}