namespace UglyToad.PdfPig.Fonts
{
    using System;
#if !NET
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// The exception thrown when an error is encountered parsing a font from the PDF document.
    /// This occurs where the format of the font program or dictionary does not meet the specification.
    /// </summary>
    /// <inheritdoc cref="Exception"/>
#if !NET
    [Serializable]
#endif
    public class InvalidFontFormatException : Exception
    {
        /// <inheritdoc />
        public InvalidFontFormatException()
        {
        }

        /// <inheritdoc />
        public InvalidFontFormatException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public InvalidFontFormatException(string message, Exception inner) : base(message, inner)
        {
        }

#if !NET
        /// <inheritdoc />
        protected InvalidFontFormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}
