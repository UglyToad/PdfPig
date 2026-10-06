namespace UglyToad.PdfPig.Content
{
    using Core;
    using Logging;
    using Parser.Parts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class PagesFactory
    {
        private sealed class PageCounter
        {
            public int PageCount { get; private set; }
            public void Increment()
            {
                PageCount++;
            }
        }

        public static Pages Create(IndirectReference pagesReference, DictionaryToken pagesDictionary, IPdfTokenScanner scanner, IPageFactory<Page> pageFactory, ILog log, bool isLenientParsing)
        {
            var pageNumber = new PageCounter();

            var pageTree = ProcessPagesNode(pagesReference, pagesDictionary, new IndirectReference(1, 0), true,
                scanner, isLenientParsing, pageNumber);

            if (!pageTree.IsRoot)
            {
                throw new ArgumentException("Page tree must be the root page tree node.", nameof(pageTree));
            }

            var pagesByNumber = new Dictionary<int, PageTreeNode>();
            PopulatePageByNumberDictionary(pageTree, pagesByNumber);

            var dictionaryPageCount = pagesDictionary.GetIntOrDefault(NameToken.Count);
            if (dictionaryPageCount != pagesByNumber.Count)
            {
                log.Warn($"Dictionary Page Count {dictionaryPageCount} different to discovered pages {pagesByNumber.Count}. Using {pagesByNumber.Count}.");
            }

            return new Pages(pageFactory, scanner, pageTree, pagesByNumber);
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

                return new PageTreeNode(nodeDictionaryInput, referenceInput, true, pageNumber.PageCount).WithChildren(Array.Empty<PageTreeNode>());
            }

            //If we got here, we have to iterate till we manage to exit

            // Every pages node is processed at most once. The former guard only remembered the last 1000
            // references, so a ring of 1001 nodes was walked forever, allocating a new node per step until
            // the process ran out of memory.
            var visited = new HashSet<IndirectReference>();

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

                if (!visited.Add(current.reference))
                {
                    continue; // don't reprocess a node already processed, breaks cycles. Issue #519
                }

                if (!current.nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken? kids))
                {
                    if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Pages node in the document pages tree did not define a kids array: {current.nodeDictionary}.");
                    }

                    kids = new ArrayToken(Array.Empty<IToken>());
                }

                foreach (var kid in kids.Data)
                {
                    DictionaryToken? kidDictionaryToken = null;
                    if (!(kid is IndirectReferenceToken kidRef))
                    {
                        throw new PdfDocumentFormatException($"Kids array contained invalid entry (must be indirect reference): {kid}.");
                    }
                    if (!DirectObjectFinder.TryGet(kidRef, pdfTokenScanner, out kidDictionaryToken))
                    {
                        if (!isLenientParsing)
                        {
                            throw new PdfDocumentFormatException($"Could not find dictionary associated with reference in pages kids array: {kidRef}.");
                        }
                    }
                    kidDictionaryToken ??= new DictionaryToken(new Dictionary<NameToken, IToken>());

                    bool isChildPage = CheckIfIsPage(kidDictionaryToken, current.reference, false, pdfTokenScanner, isLenientParsing);

                    if (isChildPage)
                    {
                        var kidPageNode =
                            new PageTreeNode(kidDictionaryToken, kidRef.Data, true, pageNumber.PageCount).WithChildren(Array.Empty<PageTreeNode>());
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

            foreach (var child in firstPage.Children!.ToRecursiveOrderList(x => x.Children!).Where(child => child.IsPage))
            {
                pageNumber.Increment();
                child.PageNumber = pageNumber.PageCount;
            }

            return firstPage;
        }

        private static bool CheckIfIsPage(DictionaryToken nodeDictionary, IndirectReference parentReference, bool isRoot, IPdfTokenScanner pdfTokenScanner, bool isLenientParsing)
        {
            var isPage = false;

            if (!nodeDictionary.TryGet(NameToken.Type, pdfTokenScanner, out NameToken? type))
            {
                if (!isLenientParsing) { throw new PdfDocumentFormatException($"Node in the document pages tree did not define a type: {nodeDictionary}."); }

                if (!nodeDictionary.TryGet(NameToken.Kids, pdfTokenScanner, out ArrayToken? _)) { isPage = true; }
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
                if (!nodeDictionary.TryGet(NameToken.Parent, pdfTokenScanner, out IndirectReferenceToken? parentReferenceToken))
                {
                    throw new PdfDocumentFormatException($"Could not find parent indirect reference token on pages tree node: {nodeDictionary}.");
                }

                if (!parentReferenceToken.Data.Equals(parentReference)) { throw new PdfDocumentFormatException($"Pages tree node parent reference {parentReferenceToken.Data} did not match actual parent {parentReference}."); }
            }

            return isPage;
        }

        private static void PopulatePageByNumberDictionary(PageTreeNode root, Dictionary<int, PageTreeNode> result)
        {
            // Iterative: a deep (non-cyclic) chain of pages nodes used to recurse until the process died
            // with a non-catchable stack overflow (about 40,000 levels on a thread pool thread).
            var pending = new Stack<PageTreeNode>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();

                if (node.IsPage)
                {
                    if (!node.PageNumber.HasValue)
                    {
                        throw new InvalidOperationException($"Node was page but did not have page number: {node}.");
                    }

                    result[node.PageNumber.Value] = node;
                    continue;
                }

                foreach (var child in node.Children!)
                {
                    pending.Push(child);
                }
            }
        }
    }
}
