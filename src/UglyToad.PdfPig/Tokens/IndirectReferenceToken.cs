namespace UglyToad.PdfPig.Tokens
{
    using CrossReference;

    /// <summary>
    /// A reference to an indirect object (see <see cref="ObjectToken"/>). This object may be located by using the
    /// <see cref="CrossReferenceTable"/>.
    /// </summary>
    public class IndirectReferenceToken : IDataToken<IndirectReference>
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

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Data}";
        }
    }
}
