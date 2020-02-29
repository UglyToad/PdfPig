namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
    using UglyToad.PdfPig.Exceptions;
    using UglyToad.PdfPig.Graphics.Operations;
    using UglyToad.PdfPig.Writer.Fonts;

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

            const bool isLenientParsing = true;

            var documentBuilder = new DocumentMerger();

            foreach (var file in files)
            {
                var inputBytes = new ByteArrayInputBytes(file);
                var coreScanner = new CoreTokenScanner(inputBytes);

                var version = FileHeaderParser.Parse(coreScanner, true, Log);

                var bruteForceSearcher = new BruteForceSearcher(inputBytes);
                var crossReferenceParser = new CrossReferenceParser(Log, new XrefOffsetValidator(Log), new XrefCosOffsetChecker(Log, bruteForceSearcher), 
                    new Parser.Parts.CrossReference.CrossReferenceStreamParser(FilterProvider));

                CrossReferenceTable crossReference = null;

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

                documentBuilder.AppendDocument(documentCatalog, pdfScanner);
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
            private MemoryStream Memory = new MemoryStream();

            private readonly BuilderContext Context = new BuilderContext();

            private readonly List<IndirectReferenceToken> DocumentPages = new List<IndirectReferenceToken>();

            private IndirectReferenceToken RootPagesIndirectReference;

            public DocumentMerger()
            {
                var reserved = Context.ReserveNumber();
                RootPagesIndirectReference = new IndirectReferenceToken(new IndirectReference(reserved, 0));

                WriteHeaderToStream();
            }

            private void WriteHeaderToStream()
            {
                // Copied from UglyToad.PdfPig.Writer.PdfDocumentBuilder
                WriteString("%PDF-1.7", Memory);

                // Files with binary data should contain a 2nd comment line followed by 4 bytes with values > 127
                Memory.WriteText("%");
                Memory.WriteByte(169);
                Memory.WriteByte(205);
                Memory.WriteByte(196);
                Memory.WriteByte(210);
                Memory.WriteNewLine();
            }
 
            public void AppendDocument(Catalog documentCatalog, IPdfTokenScanner tokenScanner)
            {
                if (Memory == null)
                {
                    throw new ObjectDisposedException("Merger disposed already");
                }

                var pagesReference = CopyPagesTree(documentCatalog.PageTree, RootPagesIndirectReference, tokenScanner);
                DocumentPages.Add(new IndirectReferenceToken(pagesReference.Number));
            }

            private ObjectToken CopyPagesTree(PageTreeNode treeNode, IndirectReferenceToken treeParentReference, IPdfTokenScanner tokenScanner)
            {
                Debug.Assert(!treeNode.IsPage);

                var currentNodeReserved = Context.ReserveNumber();
                var currentNodeReference = new IndirectReferenceToken(new IndirectReference(currentNodeReserved, 0));

                var pageReferences = new List<IndirectReferenceToken>();
                foreach (var pageNode in treeNode.Children)
                {
                    IndirectReference newEntry;
                    if (!pageNode.IsPage)
                        newEntry = CopyPagesTree(pageNode, currentNodeReference, tokenScanner).Number;
                    else 
                        newEntry = CopyPageNode(pageNode, currentNodeReference, tokenScanner).Number;

                    pageReferences.Add(new IndirectReferenceToken(newEntry));
                }

                var pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(pageReferences) },
                    { NameToken.Count, new NumericToken(pageReferences.Count) },
                    { NameToken.Parent, treeParentReference }
                });

                return Context.WriteObject(Memory, pagesDictionary, currentNodeReserved);
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

                return Context.WriteObject(Memory, new DictionaryToken(pageDictionary));
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
                    var objToken = Context.WriteObject(Memory, newToken);
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

            public byte[] Build()
            {
                if (Memory == null)
                {
                    throw new ObjectDisposedException("Merger disposed already");
                }

                if (DocumentPages.Count < 1)
                {
                    throw new PdfDocumentFormatException("Empty document");
                }

                var pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(DocumentPages) },
                    { NameToken.Count, new NumericToken(DocumentPages.Count) }
                });

                var pagesRef = Context.WriteObject(Memory, pagesDictionary, (int)RootPagesIndirectReference.Data.ObjectNumber);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, new IndirectReferenceToken(pagesRef.Number) }
                });

                var catalogRef = Context.WriteObject(Memory, catalog);

                TokenWriter.WriteCrossReferenceTable(Context.ObjectOffsets, catalogRef, Memory, null);
                
                var bytes = Memory.ToArray();

                Close();

                return bytes;
            }

            public void Close()
            {
                if (Memory == null)
                    return;

                Memory.Dispose();
                Memory = null;
            }

            // Note: This method is copied from UglyToad.PdfPig.Writer.PdfDocumentBuilder
            private static void WriteString(string text, MemoryStream stream, bool appendBreak = true)
            {
                var bytes = OtherEncodings.StringAsLatin1Bytes(text);
                stream.Write(bytes, 0, bytes.Length);
                if (appendBreak)
                {
                    stream.WriteNewLine();
                }
            }
        }
    }
}