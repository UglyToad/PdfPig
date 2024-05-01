namespace UglyToad.PdfPig.Exceptions
{
    using System;
#if !NET
    using System.Runtime.Serialization;
#endif
    using Encryption;

    /// <inheritdoc />
    /// <summary>
    /// The document is encrypted and cannot be decrypted.
    /// </summary>
#if !NET
    [Serializable]
#endif
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

#if !NET
        /// <inheritdoc />
        protected PdfDocumentEncryptedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}
