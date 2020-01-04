namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The exception thrown when an error is encountered parsing a font from the PDF document.
    /// This occurs where the format of the font program or dictionary does not meet the specification.
    /// </summary>
    /// <inheritdoc cref="Exception"/>
    [Serializable]
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

        /// <inheritdoc />
        protected InvalidFontFormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
