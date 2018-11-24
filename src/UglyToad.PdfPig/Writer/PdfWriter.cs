namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices.ComTypes;
    using Content;
    using Geometry;

    internal class PdfDocumentBuilder
    {
        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();

        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;

        public PdfPageBuilder AddPage(PageSize size, bool isPortrait)
        {
            if (!size.TryGetPdfRectangle(out var rectangle))
            {
                throw new ArgumentException($"No rectangle found for Page Size {size}.");
            }

            if (!isPortrait)
            {
                rectangle = new PdfRectangle(0, 0, rectangle.Height, rectangle.Width);
            }

            PdfPageBuilder builder = null;
            for (var i = 0; i < pages.Count; i++)
            {
                if (!pages.ContainsKey(i + 1))
                {
                    builder = new PdfPageBuilder(i + 1, this);
                    break;
                }
            }

            if (builder == null)
            {
                builder = new PdfPageBuilder(pages.Count + 1, this);
            }

            builder.PageSize = rectangle;
            pages[builder.PageNumber] = builder;

            return builder;
        }

        public void Generate(IStream stream)
        {

        }

        public void Generate(string fileName)
        {

        }
    }
}
