namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when a PDF contains an invalid compressed data stream.
    /// </summary>
    [Serializable]
    public class CorruptCompressedDataException : Exception
    {
        /// <inheritdoc />
        public CorruptCompressedDataException()
        {
        }

        /// <inheritdoc />
        public CorruptCompressedDataException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public CorruptCompressedDataException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected CorruptCompressedDataException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}