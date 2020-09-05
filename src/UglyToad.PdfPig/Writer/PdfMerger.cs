namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Content;
    using Copier;
    using Copier.Font;
    using Copier.Page;
    using Core;
    using CrossReference;
    using Encryption;
    using Filters;
    using Logging;
    using Parser;
    using Parser.FileStructure;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Exceptions;

    /// <summary>
    /// Merges PDF documents into each other.
    /// </summary>
    public static class PdfMerger
    {
        private static readonly ILog Log = new NoOpLog();

        private static readonly IFilterProvider FilterProvider = DefaultFilterProvider.Instance;

        /// <summary>
        /// Merge two PDF documents together with the pages from <paramref name="file1"/> followed by <paramref name="file2"/>.
        /// </summary>
        public static byte[] Merge(string file1, string file2)
        {
            if (file1 == null)
            {
                throw new ArgumentNullException(nameof(file1));
            }

            if (file2 == null)
            {
                throw new ArgumentNullException(nameof(file2));
            }

            return Merge(new[]
            {
                File.ReadAllBytes(file1),
                File.ReadAllBytes(file2)
            });
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(params string[] filePaths)
        {
            var bytes = new List<byte[]>(filePaths.Length);

            for (var i = 0; i < filePaths.Length; i++)
            {
                var filePath = filePaths[i];

                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePaths), $"Null filepath at index {i}.");
                }

                bytes.Add(File.ReadAllBytes(filePath));
            }

            return Merge(bytes);
        }

        /// <summary>
        /// Merge the set of PDF documents.
        /// </summary>
        public static byte[] Merge(IReadOnlyList<byte[]> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            const bool isLenientParsing = false;

            var documentBuilder = new DocumentMerger();

            foreach (var file in files)
            {
                var inputBytes = new ByteArrayInputBytes(file);
                var coreScanner = new CoreTokenScanner(inputBytes);

                var version = FileHeaderParser.Parse(coreScanner, isLenientParsing, Log);

                var crossReferenceParser = new CrossReferenceParser(Log, new XrefOffsetValidator(Log),
                    new Parser.Parts.CrossReference.CrossReferenceStreamParser(FilterProvider));

                CrossReferenceTable crossReference = null;

                // ReSharper disable once AccessToModifiedClosure
                var locationProvider = new ObjectLocationProvider(() => crossReference, inputBytes);

                var pdfScanner = new PdfTokenScanner(inputBytes, locationProvider, FilterProvider, NoOpEncryptionHandler.Instance);

                var crossReferenceOffset = FileTrailerParser.GetFirstCrossReferenceOffset(inputBytes, coreScanner, isLenientParsing);
                crossReference = crossReferenceParser.Parse(inputBytes, isLenientParsing, crossReferenceOffset, version.OffsetInFile, pdfScanner, coreScanner);

                var catalogDictionaryToken = ParseCatalog(crossReference, pdfScanner, out var encryptionDictionary);
                if (encryptionDictionary != null)
                {
                    throw new PdfDocumentEncryptedException("Unable to merge document with password");
                }

                var documentCatalog = CatalogFactory.Create(crossReference.Trailer.Root, catalogDictionaryToken, pdfScanner, isLenientParsing);

                documentBuilder.AppendDocument(documentCatalog, version.Version, pdfScanner);
            }

            return documentBuilder.Build();
        }

        // This method is a basically a copy of the method UglyToad.PdfPig.Parser.PdfDocumentFactory.ParseTrailer()
        private static DictionaryToken ParseCatalog(CrossReferenceTable crossReferenceTable,
            IPdfTokenScanner pdfTokenScanner,
            out EncryptionDictionary encryptionDictionary)
        {
            encryptionDictionary = null;

            if (crossReferenceTable.Trailer.EncryptionToken != null)
            {
                if (!DirectObjectFinder.TryGet(crossReferenceTable.Trailer.EncryptionToken, pdfTokenScanner,
                    out DictionaryToken encryptionDictionaryToken))
                {
                    throw new PdfDocumentFormatException($"Unrecognized encryption token in trailer: {crossReferenceTable.Trailer.EncryptionToken}.");
                }

                encryptionDictionary = EncryptionDictionaryFactory.Read(encryptionDictionaryToken, pdfTokenScanner);
            }

            var rootDictionary = DirectObjectFinder.Get<DictionaryToken>(crossReferenceTable.Trailer.Root, pdfTokenScanner);

            if (!rootDictionary.ContainsKey(NameToken.Type))
            {
                rootDictionary = rootDictionary.With(NameToken.Type, NameToken.Catalog);
            }

            return rootDictionary;
        }

        private class DocumentMerger
        {
            private const decimal DefaultVersion = 1.2m;

            private readonly PdfStreamWriter context;
            private readonly List<IndirectReferenceToken> pagesTokenReferences;
            private readonly IndirectReferenceToken rootPagesReference;
            private readonly MultiCopier copier;

            private decimal currentVersion = DefaultVersion;
            private int pageCount = 0;

            public DocumentMerger()
            {
                context = new PdfStreamWriter();
                pagesTokenReferences = new List<IndirectReferenceToken>();

                rootPagesReference = context.ReserveNumberToken();

                copier = new MultiCopier(context);
                copier.AddCopier(new PagesCopier(copier, rootPagesReference));
                copier.AddCopier(new FontCopier(copier));

            }
 
            public void AppendDocument(Catalog documentCatalog, decimal version, IPdfTokenScanner tokenScanner)
            {
                currentVersion = Math.Max(version, currentVersion);

                var copiedPages = copier.CopyObject(new IndirectReferenceToken(documentCatalog.PageTree.Reference), tokenScanner) as IndirectReferenceToken;
                pagesTokenReferences.Add(copiedPages);

                pageCount += documentCatalog.PagesDictionary.Get<NumericToken>(NameToken.Count, tokenScanner).Int;

                copier.ClearReference();
            }

            public byte[] Build()
            {
                if (pagesTokenReferences.Count < 1)
                {
                    throw new PdfDocumentFormatException("Empty document");
                }

                var pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(pagesTokenReferences) },
                    { NameToken.Count, new NumericToken(pageCount) }
                });

                var pagesRef = context.WriteToken( pagesDictionary, (int)rootPagesReference.Data.ObjectNumber);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, pagesRef }
                });

                var catalogRef = context.WriteToken(catalog);
                
                context.Flush(currentVersion, catalogRef);
                
                var bytes = context.ToArray();

                Close();

                return bytes;
            }

            private void Close()
            {
                context.Dispose();
            }
        }
    }
}