namespace UglyToad.Pdf.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <inheritdoc />
    /// <summary>
    /// This exception will be thrown where the contents of the PDF document do not match the specification in such a way that it
    /// renders the document unreadable.
    /// </summary>
    [Serializable]
    public class PdfDocumentFormatException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.Pdf.Exceptions.PdfDocumentFormatException" />.
        /// </summary>
        public PdfDocumentFormatException()
        {
        }

        public PdfDocumentFormatException(string message) : base(message)
        {
        }

        public PdfDocumentFormatException(string message, Exception inner) : base(message, inner)
        {
        }

        protected PdfDocumentFormatException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
