namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class CatalogFactory
    {

        private class PageCounter
        {
            public int PageCount { get; private set; }
            public void Increment()
            {
                PageCount++;
            }
        }

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

            var pageNumber = new PageCounter();

            var pageTree = ProcessPagesNode(pagesReference, pages, new IndirectReference(1, 0), true,
                scanner, isLenientParsing, pageNumber);

            return new Catalog(dictionary, pages, pageTree);
        }

#if NETSTANDARD2_0_OR_GREATER

        private static PageTreeNode ProcessPagesNode(IndirectReference referenceInput, DictionaryToken nodeDictionaryInput, IndirectReference parentReferenceInput, bool isRoot, IPdfTokenScanner pdfTokenScanner, bool isLenientParsing, PageCounter pageNumber)
        {
            bool isPage = CheckIfIsPage(nodeDictionaryInput, parentReferenceInput, isRoot, pdfTokenScanner, isLenientParsing);

            if (isPage)
            {
                pageNumber.Increment();

                return new PageTreeNode(nodeDictionaryInput, referenceInput, true, pageNumber.PageCount).WithChildren(EmptyArray<PageTreeNode>.Instance);
            }

            //If we got here, we have to iterate till we manage to exit

            var toProcess         = new Queue<(PageTreeNode thisPage, IndirectReference reference, DictionaryToken nodeDictionary, IndirectReference parentReference, List<PageTreeNode> nodeChildren)>();
            var firstPage         = new PageTreeNode(nodeDictionaryInput, referenceInput, false, null);
            var setChildren       = new List<Action>();
            var firstPageChildren = new List<PageTreeNode>();
            
            setChildren.Add(() => firstPage.WithChildren(firstPageChildren));

            toProcess.Enqueue((thisPage: firstPage, reference: referenceInput, nodeDictionary: nodeDictionaryInput, parentReference: parentReferenceInput, nodeChildren: firstPageChildren));

            do
            {
                var current = toProcess.Dequeue();

                if (!current.nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken kids))
                {
                    if (!isLenientParsing) { throw new PdfDocumentFormatException($"Pages node in the document pages tree did not define a kids array: {current.nodeDictionary}."); }

                    kids = new ArrayToken(EmptyArray<IToken>.Instance);
                }

                foreach (var kid in kids.Data)
                {
                    if (!(kid is IndirectReferenceToken kidRef)) { throw new PdfDocumentFormatException($"Kids array contained invalid entry (must be indirect reference): {kid}."); }

                    if (!DirectObjectFinder.TryGet(kidRef, pdfTokenScanner, out DictionaryToken kidDictionaryToken)) { throw new PdfDocumentFormatException($"Could not find dictionary associated with reference in pages kids array: {kidRef}."); }

                    bool isChildPage = CheckIfIsPage(kidDictionaryToken, current.reference, false, pdfTokenScanner, isLenientParsing);

                    if (isChildPage)
                    {
                        pageNumber.Increment();

                        var kidPageNode = new PageTreeNode(kidDictionaryToken, kidRef.Data, true, pageNumber.PageCount).WithChildren(EmptyArray<PageTreeNode>.Instance);
                        current.nodeChildren.Add(kidPageNode);
                    }
                    else
                    {
                        var kidChildNode = new PageTreeNode(kidDictionaryToken, kidRef.Data, false, null);
                        var kidChildren = new List<PageTreeNode>();
                        toProcess.Enqueue((thisPage: kidChildNode, reference: kidRef.Data, nodeDictionary: kidDictionaryToken, parentReference: current.reference, nodeChildren: kidChildren));

                        setChildren.Add(() => kidChildNode.WithChildren(kidChildren));

                        current.nodeChildren.Add(kidChildNode);
                    }
                }
            } while (toProcess.Count > 0);

            foreach (var action in setChildren)
            {
                action();
            }

            return firstPage;


            static bool CheckIfIsPage(DictionaryToken nodeDictionary, IndirectReference parentReference, bool isRoot, IPdfTokenScanner pdfTokenScanner, bool isLenientParsing)
            {
                var isPage = false;

                if (!nodeDictionary.TryGet(NameToken.Type, pdfTokenScanner, out NameToken type))
                {
                    if (!isLenientParsing) { throw new PdfDocumentFormatException($"Node in the document pages tree did not define a type: {nodeDictionary}."); }

                    if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken _)) { isPage = true; }
                }
                else
                {
                    isPage = type.Equals(NameToken.Page);

                    if (!isPage && !type.Equals(NameToken.Pages) && !isLenientParsing) { throw new PdfDocumentFormatException($"Node in the document pages tree defined invalid type: {nodeDictionary}."); }
                }

                if (!isLenientParsing && !isRoot)
                {
                    if (!nodeDictionary.TryGet(NameToken.Parent, pdfTokenScanner, out IndirectReferenceToken parentReferenceToken)) { throw new PdfDocumentFormatException($"Could not find parent indirect reference token on pages tree node: {nodeDictionary}."); }

                    if (!parentReferenceToken.Data.Equals(parentReference)) { throw new PdfDocumentFormatException($"Pages tree node parent reference {parentReferenceToken.Data} did not match actual parent {parentReference}."); }
                }

                return isPage;
            }
        }

#endif

        // Keep the algorithm below from throwing a StackOverflow exception. 
        // It probably should be refactored to not be recursive
        private const ushort MAX_TREE_DEPTH = 1024;

        private static PageTreeNode ProcessPagesNode(IndirectReference reference, DictionaryToken nodeDictionary, IndirectReference parentReference, bool isRoot, IPdfTokenScanner pdfTokenScanner, bool isLenientParsing, PageCounter pageNumber, int depth = 0)
        {
            depth++;

            if (depth > MAX_TREE_DEPTH) { throw new PdfDocumentFormatException($"Tree exceeded maximum depth of {MAX_TREE_DEPTH}, aborting."); }

            var isPage = false;

            if (!nodeDictionary.TryGet(NameToken.Type, pdfTokenScanner, out NameToken type))
            {
                if (!isLenientParsing) { throw new PdfDocumentFormatException($"Node in the document pages tree did not define a type: {nodeDictionary}."); }

                if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken _)) { isPage = true; }
            }
            else
            {
                isPage = type.Equals(NameToken.Page);

                if (!isPage && !type.Equals(NameToken.Pages) && !isLenientParsing) { throw new PdfDocumentFormatException($"Node in the document pages tree defined invalid type: {nodeDictionary}."); }
            }

            if (!isLenientParsing && !isRoot)
            {
                if (!nodeDictionary.TryGet(NameToken.Parent, pdfTokenScanner, out IndirectReferenceToken parentReferenceToken)) { throw new PdfDocumentFormatException($"Could not find parent indirect reference token on pages tree node: {nodeDictionary}."); }

                if (!parentReferenceToken.Data.Equals(parentReference)) { throw new PdfDocumentFormatException($"Pages tree node parent reference {parentReferenceToken.Data} did not match actual parent {parentReference}."); }
            }

            if (isPage)
            {
                pageNumber.Increment();
                var newPage = new PageTreeNode(nodeDictionary, reference, true, pageNumber.PageCount).WithChildren(EmptyArray<PageTreeNode>.Instance);
                return newPage;
            }
            
            if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken kids))
            {
                if (!isLenientParsing) { throw new PdfDocumentFormatException($"Pages node in the document pages tree did not define a kids array: {nodeDictionary}."); }
               
                kids = new ArrayToken(EmptyArray<IToken>.Instance);
            }

            var nodeChildren = new List<PageTreeNode>();

            foreach (var kid in kids.Data)
            {
                if (!(kid is IndirectReferenceToken kidRef)) { throw new PdfDocumentFormatException($"Kids array contained invalid entry (must be indirect reference): {kid}."); }

                if (!DirectObjectFinder.TryGet(kidRef, pdfTokenScanner, out DictionaryToken kidDictionaryToken)) { throw new PdfDocumentFormatException($"Could not find dictionary associated with reference in pages kids array: {kidRef}."); }

                var kidNode = ProcessPagesNode(kidRef.Data, kidDictionaryToken, reference, false, pdfTokenScanner, isLenientParsing, pageNumber, depth);

                nodeChildren.Add(kidNode);
            }

            return new PageTreeNode(nodeDictionary, reference, false, null).WithChildren(nodeChildren);
        }
    }
}
