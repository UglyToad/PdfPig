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
    }
}