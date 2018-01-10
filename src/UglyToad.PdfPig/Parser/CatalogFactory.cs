namespace UglyToad.PdfPig.Parser
{
    using System;
    using Content;
    using ContentStream;
    using Cos;
    using Exceptions;
    using IO;
    using Parts;

    internal class CatalogFactory
    {
        private readonly IPdfObjectParser pdfObjectParser;

        public CatalogFactory(IPdfObjectParser pdfObjectParser)
        {
            this.pdfObjectParser = pdfObjectParser;
        }

        public Catalog Create(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (dictionary.TryGetName(CosName.TYPE, out var type) && !type.Equals(CosName.CATALOG))
            {
                throw new PdfDocumentFormatException($"The type of the catalog dictionary was not Catalog: {dictionary}.");
            }

            if (!dictionary.TryGetItemOfType(CosName.PAGES, out CosObject value))
            {
                throw new PdfDocumentFormatException($"No pages entry was found in the catalog dictionary: {dictionary}.");
            }

            var pages = DirectObjectFinder.Find<PdfDictionary>(value, pdfObjectParser, reader, isLenientParsing);

            if (!(pages is PdfDictionary pagesDictionary))
            {
                throw new PdfDocumentFormatException($"The pages entry in the catalog {value.ToIndirectReference()} did not link to a dictionary: {pages}.");
            }

            return new Catalog(dictionary, pagesDictionary);
        }
    }
}
