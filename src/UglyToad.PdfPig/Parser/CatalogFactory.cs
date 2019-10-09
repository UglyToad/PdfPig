namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Exceptions;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class CatalogFactory
    {
        public static Catalog Create(IndirectReference rootReference, DictionaryToken dictionary, 
            IPdfTokenScanner scanner,
            bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (dictionary.TryGet(NameToken.Type, out var type) && !ReferenceEquals(type, NameToken.Catalog))
            {
                throw new PdfDocumentFormatException($"The type of the catalog dictionary was not Catalog: {dictionary}.");
            }

            if (!dictionary.TryGet(NameToken.Pages, out var value))
            {
                throw new PdfDocumentFormatException($"No pages entry was found in the catalog dictionary: {dictionary}.");
            }

            DictionaryToken pages;
            var pagesReference = rootReference;

            if (value is IndirectReferenceToken pagesRef)
            {
                pagesReference = pagesRef.Data;
                pages = DirectObjectFinder.Get<DictionaryToken>(pagesRef, scanner);
            }
            else if (value is DictionaryToken pagesDict)
            {
                pages = pagesDict;
            }
            else
            {
                pages = DirectObjectFinder.Get<DictionaryToken>(value, scanner);
            }

            var pageNumber = 0;

            var pageTree = ProcessPagesNode(pagesReference, pages, new IndirectReference(1, 0), true,
                scanner, isLenientParsing, ref pageNumber);

            return new Catalog(dictionary, pages, pageTree);
        }

        private static PageTreeNode ProcessPagesNode(IndirectReference reference, DictionaryToken nodeDictionary,
            IndirectReference parentReference,
            bool isRoot,
            IPdfTokenScanner pdfTokenScanner,
            bool isLenientParsing,
            ref int pageNumber)
        {
            var isPage = false;

            if (!nodeDictionary.TryGet(NameToken.Type, pdfTokenScanner, out NameToken type))
            {
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Node in the document pages tree did not define a type: {nodeDictionary}.");
                }

                if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken _))
                {
                    isPage = true;
                }
            }
            else
            {
                isPage = type.Equals(NameToken.Page);

                if (!isPage && !type.Equals(NameToken.Pages) && !isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Node in the document pages tree defined invalid type: {nodeDictionary}.");
                }
            }

            if (!isLenientParsing && !isRoot)
            {
                if (!nodeDictionary.TryGet(NameToken.Parent, pdfTokenScanner, out IndirectReferenceToken parentReferenceToken))
                {
                    throw new PdfDocumentFormatException($"Could not find parent indirect reference token on pages tree node: {nodeDictionary}.");
                }

                if (!parentReferenceToken.Data.Equals(parentReference))
                {
                    throw new PdfDocumentFormatException($"Pages tree node parent reference {parentReferenceToken.Data} did not match actual parent {parentReference}.");
                }
            }

            if (isPage)
            {
                pageNumber++;

                var thisNode = new PageTreeNode(nodeDictionary, reference, true,
                    pageNumber,
                    EmptyArray<PageTreeNode>.Instance);

                return thisNode;
            }
            
            if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken kids))
            {
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Pages node in the document pages tree did not define a kids array: {nodeDictionary}.");
                }
               
                kids = new ArrayToken(EmptyArray<IToken>.Instance);
            }

            var nodeChildren = new List<PageTreeNode>();

            foreach (var kid in kids.Data)
            {
                if (!(kid is IndirectReferenceToken kidRef))
                {
                    throw new PdfDocumentFormatException($"Kids array contained invalid entry (must be indirect reference): {kid}.");
                }

                if (!DirectObjectFinder.TryGet(kidRef, pdfTokenScanner, out DictionaryToken kidDictionaryToken))
                {
                    throw new PdfDocumentFormatException($"Could not find dictionary associated with reference in pages kids array: {kidRef}.");
                }

                var kidNode = ProcessPagesNode(kidRef.Data, kidDictionaryToken, reference, false, pdfTokenScanner, isLenientParsing, ref pageNumber);

                nodeChildren.Add(kidNode);
            }

            return new PageTreeNode(nodeDictionary, reference, false, null, nodeChildren);
        }
    }
}
