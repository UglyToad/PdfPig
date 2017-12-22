namespace UglyToad.Pdf
{
    using System;
    using Content;
    using Cos;
    using IO;
    using Logging;
    using Parser;
    using Parser.Parts;
    using Util;
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
        public Catalog Catalog { get; }

        [NotNull]
        public Pages Pages { get; }

        internal PdfDocument(ILog log, IRandomAccessRead reader, HeaderVersion version, CrossReferenceTable crossReferenceTable,
            bool isLenientParsing, 
            ParsingCachingProviders cachingProviders,
            IPageFactory pageFactory,
            IPdfObjectParser pdfObjectParser,
            Catalog catalog)
        {
            this.log = log;
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.crossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            this.isLenientParsing = isLenientParsing;
            this.cachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Pages = new Pages(log, Catalog, pdfObjectParser, pageFactory, reader, isLenientParsing);
        }

        public static PdfDocument Open(byte[] fileBytes, ParsingOptions options = null) => PdfDocumentFactory.Open(fileBytes, options);
        public static PdfDocument Open(string filename, ParsingOptions options = null) => PdfDocumentFactory.Open(filename, options);

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
