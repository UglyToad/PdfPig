namespace UglyToad.PdfPig.Writer
{
    using System;
    using Geometry;

    internal class PdfPageBuilder
    {
        private readonly PdfDocumentBuilder documentBuilder;

        public int PageNumber { get; }

        public PdfRectangle PageSize { get; set; }

        public PdfPageBuilder(int number, PdfDocumentBuilder documentBuilder)
        {
            this.documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
            PageNumber = number;
        }
    }
}