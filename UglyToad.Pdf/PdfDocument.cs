namespace UglyToad.Pdf
{
    using System;
    using Content;
    using Cos;
    using IO;
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
        [NotNull]
        private readonly IContainer container;
        private readonly bool isLenientParsing;
        [NotNull]
        private readonly ParsingCachingProviders cachingProviders;

        [NotNull]
        public Catalog Catalog { get; }

        [NotNull]
        public Pages Pages { get; }

        internal PdfDocument(IRandomAccessRead reader, HeaderVersion version, CrossReferenceTable crossReferenceTable, 
            IContainer container, 
            bool isLenientParsing, 
            ParsingCachingProviders cachingProviders,
            Catalog catalog)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.crossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.isLenientParsing = isLenientParsing;
            this.cachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            var arguments = new ParsingArguments(reader, crossReferenceTable, cachingProviders, container, isLenientParsing);
            Pages = new Pages(Catalog, arguments);
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
