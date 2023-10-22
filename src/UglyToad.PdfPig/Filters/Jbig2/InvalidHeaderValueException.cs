namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;

    internal class InvalidHeaderValueException : Jbig2Exception
    {
        public InvalidHeaderValueException()
        {
        }

        public InvalidHeaderValueException(string message)
            : base(message)
        {
        }

        public InvalidHeaderValueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
