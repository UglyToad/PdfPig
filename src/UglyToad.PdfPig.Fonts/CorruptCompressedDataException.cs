namespace UglyToad.PdfPig.Fonts
{
    using System;
#if !NET
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// Thrown when a PDF contains an invalid compressed data stream.
    /// </summary>
#if !NET
    [Serializable]
#endif
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

#if !NET
        /// <inheritdoc />
        protected CorruptCompressedDataException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}