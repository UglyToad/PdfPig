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
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// A class able to provides instructions for a pdf rearrangement
    /// </summary>
    public interface IPdfArrangement
    {
        /// <summary>
        /// Provides instructions for a pdf rearrangement
        /// </summary>
        /// <param name="pagesCountPerFileIndex"></param>
        /// <returns></returns>
        IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex);
    }

    /// <summary>
    /// Rearrange one or many pdfs from different pdf with specific ordering
    /// </summary>
    public static class PdfRearranger
    {
        private static readonly ILog Log = new NoOpLog();

        private static readonly IFilterProvider FilterProvider = DefaultFilterProvider.Instance;

        /// <summary>
        /// Write a new pdf file into the output stream containing the pages from the files requested in the <paramref name="arrangement"/>
        /// </summary>
        /// <param name="files"></param>
        /// <param name="arrangement"></param>
        /// <param name="output"></param>
        public static void Rearrange(IReadOnlyList<IInputBytes> files, IPdfArrangement arrangement, Stream output)
        {
            RearrangeMany(files, new[] { (arrangement, output) });
        }

        /// <summary>
        /// Write new pdf files into output streams containing the pages from the files requested in the arrangement
        /// </summary>
        /// <param name="files"></param>
        /// <param name="rearrangements"></param>
        public static void RearrangeMany(IReadOnlyList<IInputBytes> files, IEnumerable<(IPdfArrangement Arrangement, Stream Output)> rearrangements)
        {
            var contexts = GetFileContexts(files);
            var version = contexts.Max(c => c.Version);
            var pagesCountPerFile = contexts.ToDictionary(f => f.Index, f => f.TotalPages);

            foreach (var (arrangement, output) in rearrangements)
            {
                var documentBuilder = new DocumentMerger(output, version);
                foreach (var pageBundle in arrangement.GetArrangements(pagesCountPerFile))
                {
                    if (pageBundle.PageIndices.Count == 0)
                    {
                        continue;
                    }
                    documentBuilder.AppendDocumentPages(contexts[pageBundle.FileIndex], pageBundle.PageIndices);
                }

                documentBuilder.Build();
            }
        }

        private static FileContext[] GetFileContexts(IReadOnlyList<IInputBytes> files)
        {
            const bool isLenientParsing = false;

            var result = new FileContext[files.Count];
            for (var i = 0; i < files.Count; i++)
            {
                var inputBytes = files[i];
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
                result[i] = new FileContext
                {
                    Catalog = documentCatalog,
                    Scanner = pdfScanner,
                    Version = version.Version,
                    TotalPages = documentCatalog.PagesDictionary.GetIntOrDefault(NameToken.Count),
                    Index = i
                };
            }
            return result;
        }

        class FileContext
        {
            public Catalog Catalog { get; set; }
            public PdfTokenScanner Scanner { get; set; }
            public decimal Version { get; set; }
            public int TotalPages { get; set; }
            public int Index { get; set; }
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

            private readonly PdfStreamWriter context;
            private readonly List<IndirectReferenceToken> pagesTokenReferences = new List<IndirectReferenceToken>();
            private readonly IndirectReferenceToken rootPagesReference;

            private int pageCount = 0;

            private readonly Dictionary<int, Dictionary<IndirectReferenceToken, IndirectReferenceToken>> referencesFromDocumentByIndex
                 = new Dictionary<int, Dictionary<IndirectReferenceToken, IndirectReferenceToken>>();

            public DocumentMerger(Stream baseStream, decimal version)
            {
                context = new PdfStreamWriter(baseStream, version, false);
                rootPagesReference = context.ReserveNumberToken();
            }

            public void AppendDocumentPages(FileContext fileContext, IEnumerable<int> pageIndices)
            {
                if (!referencesFromDocumentByIndex.ContainsKey(fileContext.Index))
                {
                    referencesFromDocumentByIndex[fileContext.Index] = new Dictionary<IndirectReferenceToken, IndirectReferenceToken>();
                }
                var referencesFromDocument = referencesFromDocumentByIndex[fileContext.Index];

                var currentNodeReference = context.ReserveNumberToken();
                var pagesReferences = new List<IndirectReferenceToken>();
                var resources = new Dictionary<string, IToken>();

                bool HasCollision(PageTreeNode node)
                {
                    while (node != null)
                    {
                        var dictionary = node.NodeDictionary;
                        if (dictionary.TryGet(NameToken.Resources, fileContext.Scanner, out DictionaryToken resourcesDictionary))
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
                        if (dictionary.TryGet(NameToken.Resources, fileContext.Scanner, out DictionaryToken resourcesDictionary))
                        {
                            foreach (var pair in resourcesDictionary.Data)
                            {
                                resources.Add(pair.Key, CopyToken(pair.Value, fileContext.Scanner, referencesFromDocument));
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
                    pagesTokenReferences.Add(context.WriteToken(pagesDictionary, (int)currentNodeReference.Data.ObjectNumber));

                    pageCount += pagesReferences.Count;
                };

                foreach (var pageIndex in pageIndices)
                {
                    var pageNode = fileContext.Catalog.GetPageNode(pageIndex);
                    if (pagesReferences.Count >= ARTIFICIAL_NODE_LIMIT || HasCollision(pageNode))
                    {
                        CreateTree();

                        currentNodeReference = context.ReserveNumberToken();
                        pagesReferences = new List<IndirectReferenceToken>();
                        resources = new Dictionary<string, IToken>();
                    }

                    CopyEntries(pageNode.Parent);
                    pagesReferences.Add(CopyPageNode(pageNode, currentNodeReference, fileContext.Scanner, referencesFromDocument));
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

                var pagesRef = context.WriteToken(pagesDictionary, (int)rootPagesReference.Data.ObjectNumber);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, pagesRef }
                });

                context.Close(catalog);

                Close();
            }

            public void Close()
            {
                context.Dispose();
            }

            private (IndirectReferenceToken, int) CopyPagesTree(PageTreeNode treeNode, IndirectReferenceToken treeParentReference, IPdfTokenScanner tokenScanner, IDictionary<IndirectReferenceToken, IndirectReferenceToken> referencesFromDocument)
            {
                Debug.Assert(!treeNode.IsPage);

                var currentNodeReference = context.ReserveNumberToken();

                var pageReferences = new List<IndirectReferenceToken>();
                var nodeCount = 0;
                foreach (var pageNode in treeNode.Children)
                {
                    IndirectReferenceToken newEntry;
                    if (!pageNode.IsPage)
                    {
                        var count = 0;
                        (newEntry, count) = CopyPagesTree(pageNode, currentNodeReference, tokenScanner, referencesFromDocument);
                        nodeCount += count;
                    }
                    else
                    {
                        newEntry = CopyPageNode(pageNode, currentNodeReference, tokenScanner, referencesFromDocument);
                        ++nodeCount;
                    }

                    pageReferences.Add(newEntry);
                }

                var newPagesNode = new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(pageReferences) },
                    { NameToken.Count, new NumericToken(nodeCount) },
                    { NameToken.Parent, treeParentReference }
                };

                foreach (var pair in treeNode.NodeDictionary.Data)
                {
                    if (IgnoreKeyForPagesNode(pair))
                    {
                        continue;
                    }

                    newPagesNode[NameToken.Create(pair.Key)] = CopyToken(pair.Value, tokenScanner, referencesFromDocument);
                }

                var pagesDictionary = new DictionaryToken(newPagesNode);

                return (context.WriteToken(pagesDictionary, (int)currentNodeReference.Data.ObjectNumber), nodeCount);
            }

            private IndirectReferenceToken CopyPageNode(PageTreeNode pageNode, IndirectReferenceToken parentPagesObject, IPdfTokenScanner tokenScanner, IDictionary<IndirectReferenceToken, IndirectReferenceToken> referencesFromDocument)
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
            /// <param name="referencesFromDocument"></param>
            /// <returns>A reference of the token that was copied. With all the reference updated</returns>
            private IToken CopyToken(IToken tokenToCopy, IPdfTokenScanner tokenScanner, IDictionary<IndirectReferenceToken, IndirectReferenceToken> referencesFromDocument)
            {
                // This token need to be deep copied, because they could contain reference. So we have to update them.
                switch (tokenToCopy)
                {
                    case DictionaryToken dictionaryToken:
                    {
                        var newContent = new Dictionary<NameToken, IToken>();
                        foreach (var setPair in dictionaryToken.Data)
                        {
                            var name = setPair.Key;
                            var token = setPair.Value;
                            if (name == NameToken.Parent && token is IndirectReferenceToken)
                            {
                                // Skip Parent token, or stackoverflow
                                continue;
                            }
                            else
                            {
                                newContent.Add(NameToken.Create(name), CopyToken(token, tokenScanner, referencesFromDocument));
                            }
                        }

                        return new DictionaryToken(newContent);
                    }
                    case ArrayToken arrayToken:
                    {
                        var newArray = new List<IToken>(arrayToken.Length);
                        foreach (var token in arrayToken.Data)
                        {
                            newArray.Add(CopyToken(token, tokenScanner, referencesFromDocument));
                        }

                        return new ArrayToken(newArray);
                    }
                    case IndirectReferenceToken referenceToken:
                    {
                        if (referencesFromDocument.TryGetValue(referenceToken, out var newReferenceToken))
                        {
                            return newReferenceToken;
                        }

                        var tokenObject = DirectObjectFinder.Get<IToken>(referenceToken.Data, tokenScanner);

                        Debug.Assert(!(tokenObject is IndirectReferenceToken));

                        var newToken = CopyToken(tokenObject, tokenScanner, referencesFromDocument);
                        newReferenceToken = context.WriteToken(newToken);

                        referencesFromDocument.Add(referenceToken, newReferenceToken);

                        return newReferenceToken;
                    }
                    case StreamToken streamToken:
                    {
                        var properties = CopyToken(streamToken.StreamDictionary, tokenScanner, referencesFromDocument) as DictionaryToken;
                        Debug.Assert(properties != null);

                        var bytes = streamToken.Data;
                        return new StreamToken(properties, bytes);
                    }

                    case ObjectToken _:
                    {
                        // Since we don't write token directly to the stream.
                        // We can't know the offset. Therefore the token would be invalid
                        throw new NotSupportedException("Copying a Object token is not supported");
                    }
                }

                return tokenToCopy;
            }

            private static bool IgnoreKeyForPagesNode(KeyValuePair<string, IToken> token)
            {
                return string.Equals(token.Key, NameToken.Type.Data, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token.Key, NameToken.Kids.Data, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token.Key, NameToken.Count.Data, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(token.Key, NameToken.Parent.Data, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}