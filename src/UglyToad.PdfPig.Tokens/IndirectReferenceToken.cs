namespace UglyToad.PdfPig.Tokens
{
    using Core;

    /// <summary>
    /// A reference to an indirect object (see <see cref="ObjectToken"/>).
    /// </summary>
    public sealed class IndirectReferenceToken : IDataToken<IndirectReference>
    {
        /// <summary>
        /// The identifier for an object in the PDF file.
        /// </summary>
        public IndirectReference Data { get; }

        /// <summary>
        /// Create a new <see cref="IndirectReferenceToken"/>.
        /// </summary>
        /// <param name="data">The identifier for the object this references.</param>
        public IndirectReferenceToken(IndirectReference data)
        {
            Data = data;
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
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

            if (obj is not IndirectReferenceToken other)
            {
                return false;
            }

            return Data.Equals(other.Data);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Data}";
        }
    }
}
