namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The root of the document's object hierarchy. Contains references to objects defining the contents,
    /// outline, named destinations and more.
    /// </summary>
    public class Catalog
    {
        private readonly IReadOnlyDictionary<int, PageTreeNode> pagesByNumber;

        /// <summary>
        /// The catalog dictionary containing assorted information.
        /// </summary>
        [NotNull]
        public DictionaryToken CatalogDictionary { get; }

        /// <summary>
        /// Defines the page tree node which is the root of the pages tree for the document.
        /// </summary>
        [NotNull]
        public DictionaryToken PagesDictionary { get; }

        /// <summary>
        /// The page tree for this document containing all pages, page numbers and their dictionaries.
        /// </summary>
        public PageTreeNode PageTree { get; }

        /// <summary>
        /// Create a new <see cref="CatalogDictionary"/>.
        /// </summary>
        internal Catalog(DictionaryToken catalogDictionary, DictionaryToken pagesDictionary,
            PageTreeNode pageTree)
        {
            CatalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));
            PagesDictionary = pagesDictionary ?? throw new ArgumentNullException(nameof(pagesDictionary));
            PageTree = pageTree ?? throw new ArgumentNullException(nameof(pageTree));

            if (!pageTree.IsRoot)
            {
                throw new ArgumentException("Page tree must be the root page tree node.", nameof(pageTree));
            }

            var byNumber = new Dictionary<int, PageTreeNode>();
            PopulatePageByNumberDictionary(pageTree, byNumber);
            pagesByNumber = byNumber;
        }

        private static void PopulatePageByNumberDictionary(PageTreeNode node, Dictionary<int, PageTreeNode> result)
        {
            if (node.IsPage)
            {
                if (!node.PageNumber.HasValue)
                {
                    throw new InvalidOperationException($"Node was page but did not have page number: {node}.");
                }

                result[node.PageNumber.Value] = node;
                return;
            }

            foreach (var child in node.Children)
            {
                PopulatePageByNumberDictionary(child, result);
            }
        }

        internal PageTreeNode GetPageNode(int pageNumber)
        {
            if (!pagesByNumber.TryGetValue(pageNumber, out var node))
            {
                throw new InvalidOperationException($"Could not find page node by number for: {pageNumber}.");
            }

            return node;
        }
    }
}
