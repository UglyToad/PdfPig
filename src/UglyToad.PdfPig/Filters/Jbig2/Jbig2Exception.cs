namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;

    internal class Jbig2Exception : Exception
    {
        public Jbig2Exception()
        {
        }

        public Jbig2Exception(string message)
            : base(message)
        {
        }

        public Jbig2Exception(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
