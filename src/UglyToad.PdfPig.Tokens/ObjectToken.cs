namespace UglyToad.PdfPig.Tokens
{
    using System;
    using Core;

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
        public bool Equals(IToken obj)
        {
            if (this == obj)
                return true;

            if (!(obj is ObjectToken other))
            {
                return false;
            }

            // I don't think we should compare this value...
            // If this values are the same but this object are not the same (reference)
            // It means that one of the obj is not from stream or one of the object have been overwritten
            // So maybe putting an assert instead, would be a good idea?
            if (Position != other.Position)
            {
                return false;
            }

            if (!Number.Equals(other.Number))
            {
                return false;
            }

            if (!Data.Equals(other.Data))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Number: {Number}, Position: {Position}, Type: {Data.GetType().Name}";
        }
    }
}
