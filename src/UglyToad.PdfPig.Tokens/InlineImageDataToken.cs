namespace UglyToad.PdfPig.Tokens
{
    using System.Collections.Generic;

    /// <summary>
    /// Inline image data is used to embed images in PDF content streams. The content is wrapped by ID and ED tags in a BI operation.
    /// </summary>
    public class InlineImageDataToken : IDataToken<IReadOnlyList<byte>>
    {
        /// <inheritdoc />
        public IReadOnlyList<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="InlineImageDataToken"/>.
        /// </summary>
        /// <param name="data"></param>
        public InlineImageDataToken(IReadOnlyList<byte> data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (this == obj)
                return true;

            if (!(obj is InlineImageDataToken other))
            {
                return false;
            }

            if (Data.Count != other.Data.Count)
                return false;

            // Note: This maybe slow?
            // Maybe pre calculate some sort of hash and compare that?
            for (var index = 0; index < Data.Count; ++index)
            {
                if (Data[index] != other.Data[index])
                    return false;
            }

            return true;
        }
    }
}