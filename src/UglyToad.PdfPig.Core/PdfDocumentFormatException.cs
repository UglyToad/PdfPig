namespace UglyToad.PdfPig.Core
{
    using System;
#if !NET
    using System.Runtime.Serialization;
#endif

    /// <inheritdoc />
    /// <summary>
    /// This exception will be thrown where the contents of the PDF document do not match the specification in such a way that it
    /// renders the document unreadable.
    /// </summary>
#if !NET
    [Serializable]
#endif
    public class PdfDocumentFormatException : Exception
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.Exceptions.PdfDocumentFormatException" />.
        /// </summary>
        public PdfDocumentFormatException()
        {
        }

        /// <inheritdoc />
        public PdfDocumentFormatException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public PdfDocumentFormatException(string message, Exception inner) : base(message, inner)
        {
        }

#if !NET
        /// <inheritdoc />
        protected PdfDocumentFormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}
