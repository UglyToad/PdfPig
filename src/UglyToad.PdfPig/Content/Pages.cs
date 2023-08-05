namespace UglyToad.PdfPig.Content
{
    using Core;
    using Outline;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Parser;
    using Util;

    internal class Pages
    {
        private readonly ConcurrentDictionary<Type, object> pageFactoryCache = new ConcurrentDictionary<Type, object>();

        private readonly IPageFactory<Page> defaultPageFactory;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly Dictionary<int, PageTreeNode> pagesByNumber;
        public int Count => pagesByNumber.Count;

        /// <summary>
        /// The page tree for this document containing all pages, page numbers and their dictionaries.
        /// </summary>
        public PageTreeNode PageTree { get; }

        internal Pages(IPageFactory<Page> pageFactory, IPdfTokenScanner pdfScanner, PageTreeNode pageTree, Dictionary<int, PageTreeNode> pagesByNumber)
        {
            this.defaultPageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pagesByNumber = pagesByNumber;
            PageTree = pageTree;

            AddPageFactory(this.defaultPageFactory);
        }

        internal Page GetPage(int pageNumber, NamedDestinations namedDestinations, InternalParsingOptions parsingOptions) => GetPage(defaultPageFactory, pageNumber, namedDestinations, parsingOptions);

        internal TPage GetPage<TPage>(int pageNumber, NamedDestinations namedDestinations, InternalParsingOptions parsingOptions)
        {
            if (pageFactoryCache.TryGetValue(typeof(TPage), out var o) && o is IPageFactory<TPage> pageFactory)
            {
                return GetPage(pageFactory, pageNumber, namedDestinations, parsingOptions);
            }

            throw new InvalidOperationException($"Could not find {typeof(IPageFactory<TPage>)} for page type {typeof(TPage)}.");
        }

        private TPage GetPage<TPage>(IPageFactory<TPage> pageFactory, int pageNumber, NamedDestinations namedDestinations, InternalParsingOptions parsingOptions)
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

            return pageFactory.Create(
                pageNumber,
                pageNode.NodeDictionary,
                pageTreeMembers,
                namedDestinations,
                parsingOptions);
        }

        internal void AddPageFactory<TPage>(IPageFactory<TPage> pageFactory)
        {
            // TODO - throw if already exists
            pageFactoryCache.TryAdd(typeof(TPage), pageFactory);
        }

        internal void AddPageFactory<TPage>(Type type)
        {
            // TODO - check for type, should implement IPageFactory<TPage>

            if (!typeof(IPageFactory<TPage>).IsAssignableFrom(type))
            {
                throw new ArgumentException($"The type provided does not implement {typeof(IPageFactory<TPage>)}.");
            }

            var defaultPageFactory = (PageFactory)pageFactoryCache[typeof(Page)];

            // TODO - careful here - resourceStore is not thread safe
            var pageFactory = (IPageFactory<TPage>)Activator.CreateInstance(type,
                    defaultPageFactory.PdfScanner, defaultPageFactory.ResourceStore,
                    defaultPageFactory.FilterProvider, defaultPageFactory.PageContentParser,
                    defaultPageFactory.Log);
            AddPageFactory(pageFactory);
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
