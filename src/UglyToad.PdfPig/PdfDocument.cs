namespace UglyToad.PdfPig
{
    using System;
    using System.IO;
    using AcroForms;
    using Content;
    using CrossReference;
    using IO;
    using Logging;
    using Parser;
    using Tokenization.Scanner;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// Provides access to document level information for this PDF document as well as access to the <see cref="T:UglyToad.PdfPig.Content.Page"/>s contained in the document.
    /// </summary>
    public class PdfDocument : IDisposable
    {
        private bool isDisposed;
        private readonly Lazy<AcroForm> documentForm;

        private readonly bool isLenientParsing;

        [NotNull]
        private readonly HeaderVersion version;
        
        private readonly ILog log;

        private readonly IInputBytes inputBytes;

        [NotNull]
        private readonly ParsingCachingProviders cachingProviders;

        private readonly IPdfTokenScanner pdfScanner;
        
        [NotNull]
        private readonly Pages pages;

        /// <summary>
        /// The metadata associated with this document.
        /// </summary>
        [NotNull]
        public DocumentInformation Information { get; }

        /// <summary>
        /// Access to the underlying raw structure of the document. 
        /// </summary>
        [NotNull]
        public Structure Structure { get; }

        /// <summary>
        /// The version number of the PDF specification which this file conforms to, for example 1.4.
        /// </summary>
        public decimal Version => version.Version;

        /// <summary>
        /// Get the number of pages in this document.
        /// </summary>
        public int NumberOfPages => pages.Count;

        internal PdfDocument(ILog log, 
            IInputBytes inputBytes,
            HeaderVersion version, 
            CrossReferenceTable crossReferenceTable,
            bool isLenientParsing,
            ParsingCachingProviders cachingProviders,
            IPageFactory pageFactory,
            Catalog catalog,
            DocumentInformation information, IPdfTokenScanner pdfScanner,
            AcroFormFactory acroFormFactory)
        {
            this.log = log;
            this.inputBytes = inputBytes;
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.isLenientParsing = isLenientParsing;
            this.cachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            Information = information ?? throw new ArgumentNullException(nameof(information));
            pages = new Pages(log, catalog, pageFactory, isLenientParsing, pdfScanner);
            Structure = new Structure(catalog, crossReferenceTable, pdfScanner);
            documentForm = new Lazy<AcroForm>(() => acroFormFactory.GetAcroForm(catalog));
        }

        /// <summary>
        /// Creates a <see cref="PdfDocument"/> for reading from the provided file bytes.
        /// </summary>
        /// <param name="fileBytes">The bytes of the PDF file.</param>
        /// <param name="options">Optional parameters controlling parsing.</param>
        /// <returns>A <see cref="PdfDocument"/> providing access to the file contents.</returns>
        public static PdfDocument Open(byte[] fileBytes, ParsingOptions options = null) => PdfDocumentFactory.Open(fileBytes, options);
 
        /// <summary>
        /// Opens a file and creates a <see cref="PdfDocument"/> for reading from the provided file path.
        /// </summary>
        /// <param name="filePath">The full path to the file location of the PDF file.</param>
        /// <param name="options">Optional parameters controlling parsing.</param>
        /// <returns>A <see cref="PdfDocument"/> providing access to the file contents.</returns>
        public static PdfDocument Open(string filePath, ParsingOptions options = null) => PdfDocumentFactory.Open(filePath, options);

        /// <summary>
        /// Creates a <see cref="PdfDocument"/> for reading from the provided stream.
        /// The caller must manage disposing the stream. The created PdfDocument will not dispose the stream.
        /// </summary>
        /// <param name="stream">
        /// A stream of the file contents, this must support reading and seeking.
        /// The PdfDocument will not dispose of the provided stream.
        /// </param>
        /// <param name="options">Optional parameters controlling parsing.</param>
        /// <returns>A <see cref="PdfDocument"/> providing access to the file contents.</returns>
        public static PdfDocument Open(Stream stream, ParsingOptions options = null) => PdfDocumentFactory.Open(stream, options);

        /// <summary>
        /// Get the page with the specified page number (1 indexed).
        /// </summary>
        /// <param name="pageNumber">The number of the page to return, this starts from 1.</param>
        /// <returns>The page.</returns>
        public Page GetPage(int pageNumber)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access page after the document is disposed.");
            }

            log.Debug($"Accessing page {pageNumber}.");

            return pages.GetPage(pageNumber);
        }

        /// <summary>
        /// Gets the form if this document contains one.
        /// </summary>
        /// <returns>An <see cref="AcroForm"/> from the document or <see langword="null"/> if not present.</returns>
        internal AcroForm GetForm()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access the form after the document is disposed.");
            }

            return documentForm.Value;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Dispose the <see cref="T:UglyToad.PdfPig.PdfDocument" /> and close any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                inputBytes.Dispose();
            }
            catch (Exception ex)
            {
                log.Error("Failed disposing the PdfDocument due to an error.", ex);
            }
            finally
            {
                isDisposed = true;
            }
        }
    }
}
