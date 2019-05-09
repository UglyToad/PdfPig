namespace UglyToad.PdfPig.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Encryption;

    /// <inheritdoc />
    /// <summary>
    /// This exception will be thrown where the contents of the PDF document do not match the specification in such a way that it
    /// renders the document unreadable.
    /// </summary>
    [Serializable]
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

        /// <inheritdoc />
        protected PdfDocumentFormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// The document is encrypted and cannot be decrypted.
    /// </summary>
    [Serializable]
    public class PdfDocumentEncryptedException : Exception
    {
        internal EncryptionDictionary Dictionary { get; }

        /// <inheritdoc />
        public PdfDocumentEncryptedException()
        {
        }

        /// <inheritdoc />
        public PdfDocumentEncryptedException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public PdfDocumentEncryptedException(string message, Exception inner) : base(message, inner)
        {
        }

        internal PdfDocumentEncryptedException(string message, EncryptionDictionary dictionary) : base(message)
        {
            Dictionary = dictionary;
        }

        internal PdfDocumentEncryptedException(string message, EncryptionDictionary dictionary, Exception inner) : base(message, inner)
        {
            Dictionary = dictionary;
        }

        /// <inheritdoc />
        protected PdfDocumentEncryptedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
