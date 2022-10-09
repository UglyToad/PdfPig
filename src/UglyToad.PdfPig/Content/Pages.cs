namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class Pages
    {
        private readonly Catalog catalog;
        private readonly IPageFactory pageFactory;
        private readonly IPdfTokenScanner pdfScanner;

        public int Count { get; }

        internal Pages(Catalog catalog, IPageFactory pageFactory, IPdfTokenScanner pdfScanner)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));

            Count = catalog.PagesDictionary.GetIntOrDefault(NameToken.Count);
        }
        
        public Page GetPage(int pageNumber, InternalParsingOptions parsingOptions)
        {
            if (pageNumber <= 0 || pageNumber > Count)
            {
                parsingOptions.Logger.Error($"Page {pageNumber} requested but is out of range.");

                throw new ArgumentOutOfRangeException(nameof(pageNumber), 
                    $"Page number {pageNumber} invalid, must be between 1 and {Count}.");
            }

            var pageNode = catalog.GetPageNode(pageNumber);
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
                parsingOptions);
            
            return page;
        }
    }
}
