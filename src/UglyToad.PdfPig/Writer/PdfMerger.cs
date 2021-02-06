namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Content;
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
    using System.Linq;
    using Util;

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
        public static byte[] Merge(string file1, string file2, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null)
        {
            using (var output = new MemoryStream())
            {
                Merge(file1, file2, output, file1Selection, file2Selection);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge two PDF documents together with the pages from <paramref name="file1"/> followed by <paramref name="file2"/> into the output stream.
        /// </summary>
        public static void Merge(string file1, string file2, Stream output, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null)
        {
            _ = file1 ?? throw new ArgumentNullException(nameof(file1));
            _ = file2 ?? throw new ArgumentNullException(nameof(file2));

            using (var stream1 = new StreamInputBytes(File.OpenRead(file1)))
            {
                using (var stream2 = new StreamInputBytes(File.OpenRead(file2)))
                {
                    Merge(new[] { stream1, stream2 }, output, new[] { file1Selection, file2Selection });
                }
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(params string[] filePaths)
        {
            using (var output = new MemoryStream())
            {
                Merge(output, filePaths);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided into the output stream
        /// </summary>
        public static void Merge(Stream output, params string[] filePaths)
        {
            var streams = new List<StreamInputBytes>(filePaths.Length);
            try
            {
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i] ?? throw new ArgumentNullException(nameof(filePaths), $"Null filepath at index {i}.");
                    streams.Add(new StreamInputBytes(File.OpenRead(filePath), true));
                }

                Merge(streams, output, null);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Merge the set of PDF documents.
        /// </summary>
        public static byte[] Merge(IReadOnlyList<byte[]> files, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null)
        {
            _ = files ?? throw new ArgumentNullException(nameof(files));

            using (var output = new MemoryStream())
            {
                Merge(files.Select(f => new ByteArrayInputBytes(f)).ToArray(), output, pagesBundle);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge the set of PDF documents into the output stream
        /// The caller must manage disposing the stream. The created PdfDocument will not dispose the stream.
        /// <param name="streams">
        /// A list of streams for the files contents, this must support reading and seeking.
        /// </param>
        /// <param name="output">Must be writable</param>
        /// <param name="pagesBundle"></param>
        /// </summary>
        public static void Merge(IReadOnlyList<Stream> streams, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null)
        {
            _ = streams ?? throw new ArgumentNullException(nameof(streams));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            Merge(streams.Select(f => new StreamInputBytes(f, false)).ToArray(), output, pagesBundle);
        }

        private static void Merge(IReadOnlyList<IInputBytes> files, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle)
        {
            const bool isLenientParsing = false;

            var writer = new PdfStreamWriter(output, false);
            var documentBuilder = new DocumentMerger(writer);

            var maxVersion = 1.2m;
            var infos = new List<(CoreTokenScanner CoreScanner, HeaderVersion Version)>();
            foreach (var fileIndex in Enumerable.Range(0, files.Count))
            {
                var inputBytes = files[fileIndex];
                var coreScanner = new CoreTokenScanner(inputBytes);

                var version = FileHeaderParser.Parse(coreScanner, isLenientParsing, Log);
                maxVersion = Math.Max(maxVersion, version.Version);
                infos.Add((coreScanner, version));
            }
            writer.InitializePdf(maxVersion);

            foreach (var fileIndex in Enumerable.Range(0, files.Count))
            {
                IReadOnlyList<int> pages = null;
                if (pagesBundle != null && fileIndex < pagesBundle.Count)
                {
                    pages = pagesBundle[fileIndex];
                }

                var inputBytes = files[fileIndex];
                var (coreScanner, version) = infos[fileIndex];

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

                documentBuilder.AppendDocument(documentCatalog, pdfScanner, pages);
            }

            documentBuilder.Build();
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
            private const int ARTIFICIAL_NODE_LIMIT = 100;

            private readonly IPdfStreamWriter context;
            private readonly List<IndirectReferenceToken> pagesTokenReferences = new List<IndirectReferenceToken>();
            private readonly IndirectReferenceToken rootPagesReference;

            private int pageCount = 0;

            public DocumentMerger(IPdfStreamWriter writer)
            {
                context = writer;
                rootPagesReference = context.ReserveObjectNumber();
            }

            public void AppendDocument(Catalog catalog, IPdfTokenScanner tokenScanner, IReadOnlyList<int> pages)
            {
                IEnumerable<int> pageIndices;
                if (pages == null)
                {
                    var pagesCount = catalog.PagesDictionary.GetIntOrDefault(NameToken.Count);
                    if (pagesCount < 1)
                    {
                        return;
                    }

                    pageIndices = Enumerable.Range(1, pagesCount);
                }
                else if (pages.Count < 1)
                {
                    return;
                }
                else
                {
                    pageIndices = pages;
                }

                var referencesFromDocument = new Dictionary<IndirectReference, IndirectReferenceToken>();

                var currentNodeReference = context.ReserveObjectNumber();
                var pagesReferences = new List<IndirectReferenceToken>();
                var resources = new Dictionary<string, IToken>();

                bool DoesAEntryCollide(PageTreeNode node)
                {
                    while (node != null)
                    {
                        var dictionary = node.NodeDictionary;
                        if (dictionary.TryGet(NameToken.Resources, tokenScanner, out DictionaryToken resourcesDictionary))
                        {
                            var nonCollidingResources = resourcesDictionary.Data.Keys.Except(resources.Keys);
                            if (nonCollidingResources.Count() != resourcesDictionary.Data.Count)
                            {
                                // This means that at least one of the resources collided
                                return true;
                            }
                        }

                        /* TODO: How to handle?
                         *  `Rotate`
                         *  `CropBox`
                         *  `MediaBox`
                         */

                        // No colliding entry was found, in this node
                        // Keep walking up into the tree
                        node = node.Parent;
                    }

                    return false;
                }


                void CopyEntries(PageTreeNode node)
                {
                    while (node != null)
                    {
                        var dictionary = node.NodeDictionary;
                        if (dictionary.TryGet(NameToken.Resources, tokenScanner, out DictionaryToken resourcesDictionary))
                        {
                            foreach (var pair in resourcesDictionary.Data)
                            {
                                resources.Add(pair.Key, CopyToken(pair.Value, tokenScanner, referencesFromDocument));
                            }
                        }

                        /* TODO: How to handle?
                         *  `Rotate`
                         *  `CropBox`
                         *  `MediaBox`
                         */

                        // Keep walking up into the tree
                        node = node.Parent;
                    }
                }

                void CreateTree()
                {
                    if (pagesReferences.Count < 1)
                    {
                        throw new InvalidOperationException("Pages reference should always be more than 1 when executing this function");
                    }

                    var newPagesNode = new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Type, NameToken.Pages },
                        { NameToken.Kids, new ArrayToken(pagesReferences) },
                        { NameToken.Count, new NumericToken(pagesReferences.Count) },
                        { NameToken.Parent, rootPagesReference }
                    };

                    if (resources.Count > 0)
                    {
                        newPagesNode.Add(NameToken.Resources, DictionaryToken.With(resources));
                    }
                    
                    var pagesDictionary = new DictionaryToken(newPagesNode);
                    context.WriteToken(pagesDictionary, currentNodeReference);
                    pagesTokenReferences.Add(currentNodeReference);

                    pageCount += pagesReferences.Count;
                };

                foreach (var pageIndex in pageIndices)
                {
                    var pageNode = catalog.GetPageNode(pageIndex);
                    if (pagesReferences.Count >= ARTIFICIAL_NODE_LIMIT || DoesAEntryCollide(pageNode))
                    {
                        CreateTree();

                        currentNodeReference = context.ReserveObjectNumber();
                        pagesReferences = new List<IndirectReferenceToken>();
                        resources = new Dictionary<string, IToken>();
                    }

                    CopyEntries(pageNode.Parent);
                    pagesReferences.Add(CopyPageNode(pageNode, currentNodeReference, tokenScanner, referencesFromDocument));
                }

                if (pagesReferences.Count < 1)
                {
                    throw new InvalidOperationException("Pages reference couldn't be less than 1 because we have reserved a indirect reference token");
                }

                CreateTree();
            }

            public void Build()
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

                var pagesRef = context.WriteToken(pagesDictionary, rootPagesReference);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, pagesRef }
                });

                var catalogRef = context.WriteToken(catalog);

                context.CompletePdf(catalogRef);

                Close();
            }

            public void Close()
            {
                context.Dispose();
            }

            private IndirectReferenceToken CopyPageNode(PageTreeNode pageNode, IndirectReferenceToken parentPagesObject, IPdfTokenScanner tokenScanner, 
                IDictionary<IndirectReference, IndirectReferenceToken> referencesFromDocument)
            {
                Debug.Assert(pageNode.IsPage);

                var pageDictionary = new Dictionary<NameToken, IToken>
                {
                    {NameToken.Parent, parentPagesObject},
                };

                foreach (var setPair in pageNode.NodeDictionary.Data)
                {
                    var name = setPair.Key;
                    var token = setPair.Value;

                    if (name == NameToken.Parent)
                    {
                        // Skip Parent token, since we have to reassign it
                        continue;
                    }

                    pageDictionary.Add(NameToken.Create(name), CopyToken(token, tokenScanner, referencesFromDocument));
                }

                return context.WriteToken(new DictionaryToken(pageDictionary));
            }

            /// <summary>
            /// The purpose of this method is to resolve indirect reference. That mean copy the reference's content to the new document's stream
            /// and replace the indirect reference with the correct/new one
            /// </summary>
            /// <param name="tokenToCopy">Token to inspect for reference</param>
            /// <param name="tokenScanner">scanner get the content from the original document</param>
            /// <param name="referencesFromDocument">Map of previously copied</param>
            /// <returns>A reference of the token that was copied. With all the reference updated</returns>
            private IToken CopyToken(IToken tokenToCopy, IPdfTokenScanner tokenScanner, IDictionary<IndirectReference, IndirectReferenceToken> referencesFromDocument)
            {
                return WriterUtil.CopyToken(context, tokenToCopy, tokenScanner, referencesFromDocument);
            }
        }
    }
}