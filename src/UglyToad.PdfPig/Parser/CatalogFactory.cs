namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Parts;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

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

        private static PageTreeNode ProcessPagesNode(IndirectReference referenceInput,
            DictionaryToken nodeDictionaryInput,
            IndirectReference parentReferenceInput,
            bool isRoot,
            IPdfTokenScanner pdfTokenScanner,
            bool isLenientParsing,
            PageCounter pageNumber)
        {
            bool isPage = CheckIfIsPage(nodeDictionaryInput, parentReferenceInput, isRoot, pdfTokenScanner, isLenientParsing);

            if (isPage)
            {
                pageNumber.Increment();

                return new PageTreeNode(nodeDictionaryInput, referenceInput, true, pageNumber.PageCount).WithChildren(EmptyArray<PageTreeNode>.Instance);
            }



            //If we got here, we have to iterate till we manage to exit

            // Attempt to detect (and break) any infitine loop (IL) by recording the ids of the last 1000 (by default) tokens processed.
            const int InfiniteLoopWorkingWindow = 1000;
            var visitedTokens = new Dictionary<long, HashSet<int>>(); // Quick lookup containing ids (object number, generation) of tokens already processed (trimmed as we go to last 1000 (by default))
            var visitedTokensWorkingWindow = new Queue<(long ObjectNumber, int Generation)>(InfiniteLoopWorkingWindow);

            var toProcess =
                new Queue<(PageTreeNode thisPage, IndirectReference reference, DictionaryToken nodeDictionary, IndirectReference parentReference,
                    List<PageTreeNode> nodeChildren)>();
            var firstPage = new PageTreeNode(nodeDictionaryInput, referenceInput, false, null);
            var setChildren = new List<Action>();
            var firstPageChildren = new List<PageTreeNode>();

            setChildren.Add(() => firstPage.WithChildren(firstPageChildren));

            toProcess.Enqueue(
                (thisPage: firstPage, reference: referenceInput, nodeDictionary: nodeDictionaryInput, parentReference: parentReferenceInput,
                    nodeChildren: firstPageChildren));

            do
            {
                var current = toProcess.Dequeue();

                #region Break any potential infinite loop
                // Remember the last 1000 (by default) tokens and if we attempt to process again break out of loop
                var currentReferenceObjectNumber = current.reference.ObjectNumber;
                var currentReferenceGeneration = current.reference.Generation;
                if (visitedTokens.ContainsKey(currentReferenceObjectNumber))
                {
                    var generations = visitedTokens[currentReferenceObjectNumber];

                    if (generations.Contains(currentReferenceGeneration))
                    {
                        var listOfLastVisitedToken = visitedTokensWorkingWindow.ToList();
                        var indexOfCurrentTokenInListOfLastVisitedToken = listOfLastVisitedToken.IndexOf((currentReferenceObjectNumber, currentReferenceGeneration));
                        var howManyTokensBack = Math.Abs(indexOfCurrentTokenInListOfLastVisitedToken - listOfLastVisitedToken.Count); //eg initate loop is taking us back to last token or five token back
                        System.Diagnostics.Debug.WriteLine($"Break infinite loop while processing page {pageNumber.PageCount+1} tokens. Token with object number {currentReferenceObjectNumber} and generation {currentReferenceGeneration} processed {howManyTokensBack} token(s) back. ");
                        continue; // don't reprocess token already processed. break infinite loop. Issue #519
                    }
                    else
                    {
                        generations.Add(currentReferenceGeneration);
                        visitedTokens[currentReferenceObjectNumber] = generations;
                    }
                }
                else
                {
                    visitedTokens.Add(currentReferenceObjectNumber, new HashSet<int>() { currentReferenceGeneration });

                    visitedTokensWorkingWindow.Enqueue((currentReferenceObjectNumber, currentReferenceGeneration));
                    if (visitedTokensWorkingWindow.Count >= InfiniteLoopWorkingWindow)
                    {
                        var toBeRemovedFromWorkingHashset = visitedTokensWorkingWindow.Dequeue();
                        var toBeRemovedObjectNumber = toBeRemovedFromWorkingHashset.ObjectNumber;
                        var toBeRemovedGeneration = toBeRemovedFromWorkingHashset.Generation;
                        var generations = visitedTokens[toBeRemovedObjectNumber];
                        generations.Remove(toBeRemovedGeneration);
                        if (generations.Count == 0)
                        {
                            visitedTokens.Remove(toBeRemovedObjectNumber);
                        }
                        else
                        {
                            visitedTokens[toBeRemovedObjectNumber] = generations;
                        }
                    }
                }
                #endregion
                if (!current.nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken kids))
                {
                    if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Pages node in the document pages tree did not define a kids array: {current.nodeDictionary}.");
                    }

                    kids = new ArrayToken(EmptyArray<IToken>.Instance);
                }

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

                    bool isChildPage = CheckIfIsPage(kidDictionaryToken, current.reference, false, pdfTokenScanner, isLenientParsing);

                    if (isChildPage)
                    {
                        var kidPageNode =
                            new PageTreeNode(kidDictionaryToken, kidRef.Data, true, pageNumber.PageCount).WithChildren(EmptyArray<PageTreeNode>.Instance);
                        current.nodeChildren.Add(kidPageNode);
                    }
                    else
                    {
                        var kidChildNode = new PageTreeNode(kidDictionaryToken, kidRef.Data, false, null);
                        var kidChildren = new List<PageTreeNode>();
                        toProcess.Enqueue(
                            (thisPage: kidChildNode, reference: kidRef.Data, nodeDictionary: kidDictionaryToken, parentReference: current.reference,
                                nodeChildren: kidChildren));

                        setChildren.Add(() => kidChildNode.WithChildren(kidChildren));

                        current.nodeChildren.Add(kidChildNode);
                    }
                }
            } while (toProcess.Count > 0);

            foreach (var action in setChildren)
            {
                action();
            }

            foreach (var child in firstPage.Children.ToRecursiveOrderList(x=>x.Children).Where(child => child.IsPage))
            {
                pageNumber.Increment();
                child.PageNumber = pageNumber.PageCount;
            }

            return firstPage;
        }

        private static bool CheckIfIsPage(DictionaryToken nodeDictionary, IndirectReference parentReference, bool isRoot, IPdfTokenScanner pdfTokenScanner, bool isLenientParsing)
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
}
