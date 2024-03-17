namespace UglyToad.PdfPig.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Encryption;

    /// <inheritdoc />
    /// <summary>
    /// The document is encrypted and cannot be decrypted.
    /// </summary>
    [Serializable]
    public class PdfDocumentEncryptedException : Exception
    {
        internal EncryptionDictionary? Dictionary { get; }

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
