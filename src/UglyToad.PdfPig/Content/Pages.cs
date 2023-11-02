namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Outline.Destinations;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Pages
    {
        private readonly IPageFactory<Page> pageFactory;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly Dictionary<int, PageTreeNode> pagesByNumber;

        public int Count => pagesByNumber.Count;

        /// <summary>
        /// The page tree for this document containing all pages, page numbers and their dictionaries.
        /// </summary>
        public PageTreeNode PageTree { get; }

        internal Pages(IPageFactory<Page> pageFactory, IPdfTokenScanner pdfScanner, PageTreeNode pageTree, Dictionary<int, PageTreeNode> pagesByNumber)
        {
            this.pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pagesByNumber = pagesByNumber;
            PageTree = pageTree;
        }

        internal Page GetPage(int pageNumber, NamedDestinations namedDestinations, ParsingOptions parsingOptions)
        {
            if (pageNumber <= 0 || pageNumber > Count)
            {
                parsingOptions.Logger.Error($"Page {pageNumber} requested but is out of range.");

                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                    $"Page number {pageNumber} invalid, must be between 1 and {Count}.");
            }

            var pageNode = GetPageNode(pageNumber);
            var pageStack = new Stack<PageTreeNode>();

            var currentNode = pageNode;
            while (currentNode != null)
            {
                pageStack.Push(currentNode);
                currentNode = currentNode.Parent;
            }

            var pageTreeMembers = new PageTreeMembers();

            while (pageStack.Count > 0)
            {
                currentNode = pageStack.Pop();

                if (currentNode.NodeDictionary.TryGet(NameToken.Resources, pdfScanner, out DictionaryToken resourcesDictionary))
                {
                    pageTreeMembers.ParentResources.Enqueue(resourcesDictionary);
                }

                if (currentNode.NodeDictionary.TryGet(NameToken.MediaBox, pdfScanner, out ArrayToken mediaBox))
                {
                    pageTreeMembers.MediaBox = new MediaBox(mediaBox.ToRectangle(pdfScanner));
                }

                if (currentNode.NodeDictionary.TryGet(NameToken.Rotate, pdfScanner, out NumericToken rotateToken))
                {
                    pageTreeMembers.Rotation = rotateToken.Int;
                }
            }

            var page = pageFactory.Create(
                pageNumber,
                pageNode.NodeDictionary,
                pageTreeMembers,
                namedDestinations);

            return page;
        }

        internal PageTreeNode GetPageNode(int pageNumber)
        {
            if (!pagesByNumber.TryGetValue(pageNumber, out var node))
            {
                throw new InvalidOperationException($"Could not find page node by number for: {pageNumber}.");
            }

            return node;
        }

        internal PageTreeNode GetPageByReference(IndirectReference reference)
        {
            foreach (var page in pagesByNumber)
            {
                if (page.Value.Reference.Equals(reference))
                {
                    return page.Value;
                }
            }

            return null;
        }
    }
}
