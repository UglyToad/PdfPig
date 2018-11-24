namespace UglyToad.PdfPig.Tokens
{
    using System;
    using ContentStream;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Any object in a PDF file may be labeled as an indirect object. This gives the object a unique object identifier by which other objects can refer to it.
    /// These objects contain inner data of any type.
    /// </summary>
    public class ObjectToken : IDataToken<IToken>
    {
        /// <summary>
        /// The offset to the start of the object number from the start of the file in bytes.
        /// </summary>
        public long Position { get; }

        /// <summary>
        /// The object and generation number of the object.
        /// </summary>
        public IndirectReference Number { get; }

        /// <summary>
        /// The inner data of the object.
        /// </summary>
        [NotNull]
        public IToken Data { get; }

        /// <summary>
        /// Create a new <see cref="ObjectToken"/> from the PDF document at the given offset with the identifier and inner data.
        /// </summary>
        /// <param name="position">The offset in bytes from the start of the file for this object.</param>
        /// <param name="number">The identifier for this object.</param>
        /// <param name="data">The data contained in this object.</param>
        public ObjectToken(long position, IndirectReference number, IToken data)
        {
            Position = position;
            Number = number;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Number: {Number}, Position: {Position}, Type: {Data.GetType().Name}";
        }
    }
}
