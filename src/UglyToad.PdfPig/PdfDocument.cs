namespace UglyToad.PdfPig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using AcroForms;
    using Content;
    using CrossReference;
    using Encryption;
    using Exceptions;
    using Filters;
    using IO;
    using Logging;
    using Parser;
    using Tokenization.Scanner;
    using Tokens;
    using Outline;
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

        [CanBeNull]
        private readonly EncryptionDictionary encryptionDictionary;

        [NotNull]
        private readonly IPdfTokenScanner pdfScanner;

        private readonly IFilterProvider filterProvider;
        private readonly BookmarksProvider bookmarksProvider;

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

        /// <summary>
        /// Whether the document content is encrypted.
        /// </summary>
        public bool IsEncrypted => encryptionDictionary != null;

        internal PdfDocument(ILog log, 
            IInputBytes inputBytes,
            HeaderVersion version, 
            CrossReferenceTable crossReferenceTable,
            bool isLenientParsing,
            ParsingCachingProviders cachingProviders,
            IPageFactory pageFactory,
            Catalog catalog,
            DocumentInformation information, 
            EncryptionDictionary encryptionDictionary,
            IPdfTokenScanner pdfScanner,
            IFilterProvider filterProvider,
            AcroFormFactory acroFormFactory,
            BookmarksProvider bookmarksProvider)
        {
            this.log = log;
            this.inputBytes = inputBytes;
            this.version = version ?? throw new ArgumentNullException(nameof(version));
            this.isLenientParsing = isLenientParsing;
            this.cachingProviders = cachingProviders ?? throw new ArgumentNullException(nameof(cachingProviders));
            this.encryptionDictionary = encryptionDictionary;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.bookmarksProvider = bookmarksProvider ?? throw new ArgumentNullException(nameof(bookmarksProvider));
            Information = information ?? throw new ArgumentNullException(nameof(information));
            pages = new Pages(catalog, pageFactory, isLenientParsing, pdfScanner);
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

            try
            {
                return pages.GetPage(pageNumber);
            }
            catch (Exception ex)
            {
                if (IsEncrypted)
                {
                    throw new PdfDocumentEncryptedException("Document was encrypted which may have caused error when retrieving page.", encryptionDictionary, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Gets all pages in this document in order.
        /// </summary>
        public IEnumerable<Page> GetPages()
        {
            for (var i = 0; i < NumberOfPages; i++)
            {
                yield return GetPage(i + 1);
            }
        }

        /// <summary>
        /// Get the document level metadata if present.
        /// The metadata is XML in the (Extensible Metadata Platform) XMP format.
        /// </summary>
        /// <remarks>This will throw a <see cref="ObjectDisposedException"/> if called on a disposed <see cref="PdfDocument"/>.</remarks>
        /// <param name="metadata">The metadata stream if it exists.</param>
        /// <returns><see langword="true"/> if the metadata is present, <see langword="false"/> otherwise.</returns>
        public bool TryGetXmpMetadata(out XmpMetadata metadata)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access the document metadata after the document is disposed.");
            }

            metadata = null;

            if (!Structure.Catalog.CatalogDictionary.TryGet(NameToken.Metadata, pdfScanner, out StreamToken xmpStreamToken))
            {
                return false;
            }

            metadata = new XmpMetadata(xmpStreamToken, filterProvider);

            return true;
        }

        /// <summary>
        /// Gets the bookmarks if this document contains some.
        /// </summary>
        /// <remarks>This will throw a <see cref="ObjectDisposedException"/> if called on a disposed <see cref="PdfDocument"/>.</remarks>
        public bool TryGetBookmarks(out Bookmarks bookmarks)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access the bookmarks after the document is disposed.");
            }

            bookmarks = bookmarksProvider.GetBookmarks(Structure.Catalog);
            if (bookmarks != null) return true;
            return false;
        }

        /// <summary>
        /// Gets the form if this document contains one.
        /// </summary>
        /// <remarks>This will throw a <see cref="ObjectDisposedException"/> if called on a disposed <see cref="PdfDocument"/>.</remarks>
        /// <returns>An <see cref="AcroForm"/> from the document or <see langword="null"/> if not present.</returns>
        public bool TryGetForm(out AcroForm form)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("Cannot access the form after the document is disposed.");
            }

            form = documentForm.Value;

            return form != null;
        }
        
        /// <inheritdoc />
        /// <summary>
        /// Dispose the <see cref="T:UglyToad.PdfPig.PdfDocument" /> and close any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                pdfScanner.Dispose();
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
