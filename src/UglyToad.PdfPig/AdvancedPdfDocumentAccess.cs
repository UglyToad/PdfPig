namespace UglyToad.PdfPig
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Core;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Provides access to rare or advanced features from the PDF specification.
    /// </summary>
    public class AdvancedPdfDocumentAccess : IDisposable
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly ILookupFilterProvider filterProvider;
        private readonly Catalog catalog;

        private bool isDisposed;

        internal AdvancedPdfDocumentAccess(IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            Catalog catalog)
        {
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Get any embedded files contained in this PDF document.
        /// Since PDF 1.3 any external file referenced by the document may have its contents embedded within the referring PDF file, 
        /// allowing its contents to be stored or transmitted along with the PDF file.
        /// </summary>
        /// <param name="embeddedFiles">The set of embedded files in this document.</param>
        /// <returns><see langword="true"/> if this document contains more than zero embedded files, otherwise <see langword="false"/>.</returns>
        public bool TryGetEmbeddedFiles([NotNullWhen(true)] out IReadOnlyList<EmbeddedFile>? embeddedFiles)
        {
            GuardDisposed();

            embeddedFiles = null;

            if (!catalog.CatalogDictionary.TryGet(NameToken.Names, pdfScanner, out DictionaryToken? namesDictionary)
                || !namesDictionary.TryGet(NameToken.EmbeddedFiles, pdfScanner, out DictionaryToken? embeddedFileNamesDictionary))
            {
                return false;
            }

            var embeddedFileNames = NameTreeParser.FlattenNameTreeToDictionary(embeddedFileNamesDictionary, pdfScanner, x => x);

            if (embeddedFileNames.Count == 0)
            {
                return false;
            }

            var result = new List<EmbeddedFile>();

            foreach (var keyValuePair in embeddedFileNames)
            {
                if (!DirectObjectFinder.TryGet(keyValuePair.Value, pdfScanner, out DictionaryToken? fileDescriptorDictionaryToken)
                    || !fileDescriptorDictionaryToken.TryGet(NameToken.Ef, pdfScanner, out DictionaryToken? efDictionary)
                    || !efDictionary.TryGet(NameToken.F, pdfScanner, out StreamToken? fileStreamToken))
                {
                    continue;
                }

                var fileSpecification = string.Empty;
                if (fileDescriptorDictionaryToken.TryGet(NameToken.F, pdfScanner, out IDataToken<string>? fileSpecificationToken))
                {
                    fileSpecification = fileSpecificationToken.Data;
                }

                var fileBytes = fileStreamToken.Decode(filterProvider, pdfScanner);

                result.Add(new EmbeddedFile(keyValuePair.Key, fileSpecification, fileBytes, fileStreamToken));
            }

            embeddedFiles = result;

            return embeddedFiles.Count > 0;
        }

        /// <summary>
        /// Replaces the token in an internal cache that will be returned instead of
        /// scanning the source PDF data for future requests.
        /// </summary>
        /// <param name="reference">The object number for the object to replace.</param>
        /// <param name="replacer">Func that takes existing token as input and return new token.</param>
        public void ReplaceIndirectObject(IndirectReference reference, Func<IToken, IToken> replacer)
        {
            var obj = pdfScanner.Get(reference);
            var replacement = replacer(obj.Data);
            pdfScanner.ReplaceToken(reference, replacement);
        }

        /// <summary>
        /// Replaces the token in an internal cache that will be returned instead of
        /// scanning the source PDF data for future requests.
        /// </summary>
        /// <param name="reference">The object number for the object to replace.</param>
        /// <param name="replacement">Replacement token to use.</param>
        public void ReplaceIndirectObject(IndirectReference reference, IToken replacement)
        {
            pdfScanner.ReplaceToken(reference, replacement);
        }

        private void GuardDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(AdvancedPdfDocumentAccess));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            pdfScanner?.Dispose();
            isDisposed = true;
        }
    }
}