namespace UglyToad.Pdf
{
    using System;
    using Content;
    using Cos;
    using IO;
    using Logging;
    using Parser;
    using Parser.Parts;
    using Util.JetBrains.Annotations;

    public class PdfDocument : IDisposable
    {
        [NotNull]
        private readonly IRandomAccessRead reader;
        [NotNull]
        private readonly HeaderVersion version;
        [NotNull]
        private readonly CrossReferenceTable crossReferenceTable;

        private readonly ILog log;
        private readonly bool isLenientParsing;
        [NotNull]
        private readonly ParsingCachingProviders cachingProviders;

        [NotNull]
        internal Catalog Catalog { get; }

        [NotNull]
        internal Pages Pages { get; }

        [NotNull]
        public DocumentInformation Information { get; }

        /// <summary>
        /// The version number of the PDF specification which this file conforms to, for example 1.4.
        /// </summary>
        public decimal Version => version.Version;

        /// <summary>
        /// Get the number of pages in this document.
        /// </summary>
        public int NumberOfPages => Pages.Count;

        internal PdfDocument(ILog log, IRandomAccessRead reader, HeaderVersion version, CrossReferenceTable crossReferenceTable,
            bool isLenientParsing, 
            ParsingCachingProviders cachingProviders,
            IPageFactory pageFactory,
            IPdfObjectParser pdfObjectParser,
            Catalog catalog,
            DocumentInformation information)
        {
            this.log = log;
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.crossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            this.isLenientParsing = isLenientParsing;
            this.cachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            Information = information ?? throw new ArgumentNullException(nameof(information));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Pages = new Pages(log, Catalog, pdfObjectParser, pageFactory, reader, isLenientParsing);
        }

        public static PdfDocument Open(byte[] fileBytes, ParsingOptions options = null) => PdfDocumentFactory.Open(fileBytes, options);
        public static PdfDocument Open(string filename, ParsingOptions options = null) => PdfDocumentFactory.Open(filename, options);

        /// <summary>
        /// Get the page with the specified page number.
        /// </summary>
        /// <param name="pageNumber">The number of the page to return, this starts from 1.</param>
        /// <returns>The page.</returns>
        public Page GetPage(int pageNumber)
        {
            return Pages.GetPage(pageNumber);
        }

        public void Dispose()
        {
            try
            {
                reader.Dispose();
            }
            catch
            {
                // TODO: something
            }
        }
    }
}
