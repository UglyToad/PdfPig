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
    using Graphics.Operations;
    using Fonts;

    /// <summary>
    /// Merges PDF documents into each other.
    /// </summary>
    public static class PdfMerger
    {
        private static readonly ILog Log = new NoOpLog();

        private static readonly IFilterProvider FilterProvider = new MemoryFilterProvider(new DecodeParameterResolver(Log),
            new PngPredictor(), Log);

        /// <summary>
        /// Merge two PDF documents together with the pages from <see cref="file1"/>
        /// followed by <see cref="file2"/>.
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

                var bruteForceSearcher = new BruteForceSearcher(inputBytes);
                var crossReferenceParser = new CrossReferenceParser(Log, new XrefOffsetValidator(Log), new XrefCosOffsetChecker(Log, bruteForceSearcher), 
                    new Parser.Parts.CrossReference.CrossReferenceStreamParser(FilterProvider));

                CrossReferenceTable crossReference = null;

                // ReSharper disable once AccessToModifiedClosure
                var locationProvider = new ObjectLocationProvider(() => crossReference, bruteForceSearcher);

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
            
            private readonly BuilderContext context = new BuilderContext();
            private readonly List<IndirectReferenceToken> documentPages = new List<IndirectReferenceToken>();
            private readonly IndirectReferenceToken rootPagesIndirectReference;

            private decimal currentVersion = DefaultVersion;
            private MemoryStream memory = new MemoryStream();

            public DocumentMerger()
            {
                var reserved = context.ReserveNumber();
                rootPagesIndirectReference = new IndirectReferenceToken(new IndirectReference(reserved, 0));

                WriteHeaderToStream();
            }
 
            public void AppendDocument(Catalog documentCatalog, decimal version, IPdfTokenScanner tokenScanner)
            {
                if (memory == null)
                {
                    throw new ObjectDisposedException("Merger closed already");
                }

                currentVersion = Math.Max(version, currentVersion);

                var pagesReference = CopyPagesTree(documentCatalog.PageTree, rootPagesIndirectReference, tokenScanner);
                documentPages.Add(new IndirectReferenceToken(pagesReference.Number));
            }

            public byte[] Build()
            {
                if (memory == null)
                {
                    throw new ObjectDisposedException("Merger closed already");
                }

                if (documentPages.Count < 1)
                {
                    throw new PdfDocumentFormatException("Empty document");
                }

                var pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(documentPages) },
                    { NameToken.Count, new NumericToken(documentPages.Count) }
                });

                var pagesRef = context.WriteObject(memory, pagesDictionary, (int)rootPagesIndirectReference.Data.ObjectNumber);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, new IndirectReferenceToken(pagesRef.Number) }
                });

                var catalogRef = context.WriteObject(memory, catalog);

                TokenWriter.WriteCrossReferenceTable(context.ObjectOffsets, catalogRef, memory, null);
                
                if (currentVersion != DefaultVersion)
                {
                    memory.Seek(0, SeekOrigin.Begin);
                    WriteHeaderToStream();
                }

                var bytes = memory.ToArray();

                Close();

                return bytes;
            }

            public void Close()
            {
                memory?.Dispose();
                memory = null;
            }

            private ObjectToken CopyPagesTree(PageTreeNode treeNode, IndirectReferenceToken treeParentReference, IPdfTokenScanner tokenScanner)
            {
                Debug.Assert(!treeNode.IsPage);

                var currentNodeReserved = context.ReserveNumber();
                var currentNodeReference = new IndirectReferenceToken(new IndirectReference(currentNodeReserved, 0));

                var pageReferences = new List<IndirectReferenceToken>();
                foreach (var pageNode in treeNode.Children)
                {
                    ObjectToken newEntry;
                    if (!pageNode.IsPage)
                    {
                        newEntry = CopyPagesTree(pageNode, currentNodeReference, tokenScanner);
                    }
                    else
                    {
                        newEntry = CopyPageNode(pageNode, currentNodeReference, tokenScanner);
                    }
                    
                    pageReferences.Add(new IndirectReferenceToken(newEntry.Number));
                }

                var newPagesNode = new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(pageReferences) },
                    { NameToken.Count, new NumericToken(pageReferences.Count) },
                    { NameToken.Parent, treeParentReference }
                };

                foreach (var pair in treeNode.NodeDictionary.Data)
                {
                    if (IgnoreKeyForPagesNode(pair))
                    {
                        continue;
                    }

                    newPagesNode[NameToken.Create(pair.Key)] = CopyToken(pair.Value, tokenScanner);
                }

                var pagesDictionary = new DictionaryToken(newPagesNode);

                return context.WriteObject(memory, pagesDictionary, currentNodeReserved);
            }

            private ObjectToken CopyPageNode(PageTreeNode pageNode, IndirectReferenceToken parentPagesObject, IPdfTokenScanner tokenScanner)
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

                    pageDictionary.Add(NameToken.Create(name), CopyToken(token, tokenScanner));
                }

                return context.WriteObject(memory, new DictionaryToken(pageDictionary));
            }

            /// <summary>
            /// The purpose of this method is to resolve indirect reference. That mean copy the reference's content to the new document's stream
            /// and replace the indirect reference with the correct/new one
            /// </summary>
            /// <param name="tokenToCopy">Token to inspect for reference</param>
            /// <param name="tokenScanner">scanner get the content from the original document</param>
            /// <returns>A copy of the token with all his content copied to the new document's stream</returns>
            private IToken CopyToken(IToken tokenToCopy, IPdfTokenScanner tokenScanner)
            {
                if (tokenToCopy is DictionaryToken dictionaryToken)
                {
                    var newContent = new Dictionary<NameToken, IToken>();
                    foreach (var setPair in dictionaryToken.Data)
                    {
                        var name = setPair.Key;
                        var token = setPair.Value;
                        newContent.Add(NameToken.Create(name), CopyToken(token, tokenScanner));
                    }

                    return new DictionaryToken(newContent);
                }
                else if (tokenToCopy is ArrayToken arrayToken)
                {
                    var newArray = new List<IToken>(arrayToken.Length);
                    foreach (var token in arrayToken.Data)
                    {
                        newArray.Add(CopyToken(token, tokenScanner));
                    }

                    return new ArrayToken(newArray);
                }
                else if (tokenToCopy is IndirectReferenceToken referenceToken)
                {
                    var tokenObject = DirectObjectFinder.Get<IToken>(referenceToken.Data, tokenScanner);

                    Debug.Assert(!(tokenObject is IndirectReferenceToken));

                    var newToken = CopyToken(tokenObject, tokenScanner);
                    var objToken = context.WriteObject(memory, newToken);
                    return new IndirectReferenceToken(objToken.Number);
                }
                else if (tokenToCopy is StreamToken streamToken)
                {
                    var properties = CopyToken(streamToken.StreamDictionary, tokenScanner) as DictionaryToken;
                    Debug.Assert(properties != null);
                    return new StreamToken(properties, new List<byte>(streamToken.Data));
                }
                else // Non Complex Token - BooleanToken, NumericToken, NameToken, Etc...
                {
                    return tokenToCopy;
                }
            }

            private void WriteHeaderToStream()
            {
                WriteString($"%PDF-{currentVersion:0.0}", memory);

                memory.WriteText("%");
                memory.WriteByte(169);
                memory.WriteByte(205);
                memory.WriteByte(196);
                memory.WriteByte(210);
                memory.WriteNewLine();
            }

            private static void WriteString(string text, Stream stream)
            {
                var bytes = OtherEncodings.StringAsLatin1Bytes(text);
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteNewLine();
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