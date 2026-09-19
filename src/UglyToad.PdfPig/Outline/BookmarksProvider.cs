namespace UglyToad.PdfPig.Outline
{
    using Actions;
    using Content;
    using Destinations;
    using Logging;
    using Parser.Parts;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class BookmarksProvider
    {
        private readonly ILog log;
        private readonly IPdfTokenScanner pdfScanner;

        public BookmarksProvider(ILog log, IPdfTokenScanner pdfScanner)
        {
            this.log = log;
            this.pdfScanner = pdfScanner;
        }

        /// <summary>
        /// Extract bookmarks, if any.
        /// </summary>
        public Bookmarks? GetBookmarks(Catalog catalog,bool allowContainerNode = false)
        {
            if (!catalog.CatalogDictionary.TryGet(NameToken.Outlines, pdfScanner, out DictionaryToken? outlinesDictionary))
            {
                return null;
            }

            if (outlinesDictionary.TryGet(NameToken.Type, pdfScanner, out NameToken? typeName) && typeName != NameToken.Outlines)
            {
                log?.Error($"Outlines (bookmarks) dictionary did not have correct type specified: {typeName}.");
            }

            if (!outlinesDictionary.TryGet(NameToken.First, pdfScanner, out DictionaryToken? next))
            {
                return null;
            }

            var roots = new List<BookmarkNode>();
            var seen = new HashSet<IndirectReference>();

            // Record the first node before reading it
            if (outlinesDictionary.TryGet(NameToken.First, out IndirectReferenceToken firstReference))
            {
                seen.Add(firstReference.Data);
            }

            while (next is not null)
            {
                ReadBookmarks(next, 0, false, seen, catalog.NamedDestinations, roots, allowContainerNode);

                if (!next.TryGet(NameToken.Next, out IndirectReferenceToken nextReference)
                    || !seen.Add(nextReference.Data))
                {
                    break;
                }

                next = DirectObjectFinder.Get<DictionaryToken>(nextReference, pdfScanner);
            }

            return new Bookmarks(roots);
        }

        /// <summary>
        /// Extract bookmarks for an outline node and its descendants.
        /// </summary>
        private void ReadBookmarks(DictionaryToken nodeDictionary, int level, bool readSiblings, HashSet<IndirectReference> seen,
            NamedDestinations namedDestinations, List<BookmarkNode> list, bool allowContainerNode = false)
        {
            // 12.3 Document-Level Navigation

            var stack = new Stack<OutlineNodeState>();
            stack.Push(new OutlineNodeState(nodeDictionary, level, readSiblings, list));

            while (stack.Count > 0)
            {
                var state = stack.Peek();

                if (!state.ChildrenRead)
                {
                    state.ChildrenRead = true;

                    // 12.3.3 Document Outline - Title
                    // (Required) The text that shall be displayed on the screen for this item.
                    if (!state.Node.TryGetOptionalStringDirect(NameToken.Title, pdfScanner, out var title))
                    {
                        throw new PdfDocumentFormatException($"Invalid title for outline (bookmark) node: {state.Node}.");
                    }

                    state.Title = title;

                    // The children are read first, the bookmark for this node cannot be created until they are known.
                    if (TryGetChild(state.Node, seen, out var firstChild))
                    {
                        stack.Push(new OutlineNodeState(firstChild, state.Level + 1, true, state.Children));
                        continue;
                    }
                }

                // The children of this node are complete so the node itself can be created.
                var bookmark = CreateBookmark(state.Node, state.Title, state.Level, state.Children, namedDestinations, allowContainerNode);

                if (bookmark is not null)
                {
                    state.Output.Add(bookmark);
                }

                if (state.IsChainHead)
                {
                    state.IsChainHead = false;

                    if (!state.ReadSiblings)
                    {
                        stack.Pop();
                        continue;
                    }
                }

                // Walk all siblings of the node this chain started from, reusing the state for each of them.
                if (!TryGetSibling(state.Node, seen, out var sibling))
                {
                    stack.Pop();
                    continue;
                }

                state.MoveTo(sibling);
            }
        }

        /// <summary>
        /// Create the bookmark for a single outline node, or <see langword="null"/> where it has no usable
        /// destination or action.
        /// </summary>
        private BookmarkNode? CreateBookmark(DictionaryToken nodeDictionary, string title, int level, List<BookmarkNode> children,
            NamedDestinations namedDestinations, bool allowContainerNode)
        {
            if (DestinationProvider.TryGetDestination(nodeDictionary, NameToken.Dest, namedDestinations, pdfScanner, log, false, out var destination))
            {
                return new DocumentBookmarkNode(title, level, destination, children);
            }

            if (ActionProvider.TryGetAction(nodeDictionary, namedDestinations, pdfScanner, log, out var actionResult))
            {
                if (actionResult is GoToRAction goToRAction)
                {
                    return new ExternalBookmarkNode(title, level, goToRAction.Destination, children, goToRAction.Filename);
                }

                if (actionResult is GoToAction goToAction)
                {
                    return new DocumentBookmarkNode(title, level, goToAction.Destination, children);
                }

                if (actionResult is UriAction uriAction)
                {
                    return new UriBookmarkNode(title, level, uriAction.Uri, children);
                }

                return null;
            }

            if (allowContainerNode)
            {
                log.Warn($"No /Dest(ination) or /A(ction) entry found for bookmark node: {nodeDictionary}.");
                return new ContainerBookmarkNode(title, level, children);
            }

            log.Error($"No /Dest(ination) or /A(ction) entry found for bookmark node: {nodeDictionary}.");
            return null;
        }

        /// <summary>
        /// Get the node referenced by the /First entry, recording it in <paramref name="seen"/> so that a cyclic
        /// chain of children terminates.
        /// </summary>
        private bool TryGetChild(DictionaryToken nodeDictionary, HashSet<IndirectReference> seen, [NotNullWhen(true)] out DictionaryToken? child)
        {
            child = null;

            if (!nodeDictionary.TryGet(NameToken.First, out IToken firstToken))
            {
                return false;
            }

            if (firstToken is IndirectReferenceToken reference && !seen.Add(reference.Data))
            {
                return false;
            }

            return DirectObjectFinder.TryGet(firstToken, pdfScanner, out child);
        }

        /// <summary>
        /// Get the node referenced by the /Next entry, recording it in <paramref name="seen"/> so that a cyclic
        /// chain of siblings terminates.
        /// </summary>
        private bool TryGetSibling(DictionaryToken nodeDictionary, HashSet<IndirectReference> seen, [NotNullWhen(true)] out DictionaryToken? sibling)
        {
            sibling = null;

            if (!nodeDictionary.TryGet(NameToken.Next, out IndirectReferenceToken nextReference)
                || !seen.Add(nextReference.Data))
            {
                return false;
            }

            sibling = DirectObjectFinder.Get<DictionaryToken>(nextReference, pdfScanner);

            return sibling is not null;
        }

        private sealed class OutlineNodeState
        {
            public OutlineNodeState(DictionaryToken node, int level, bool readSiblings, List<BookmarkNode> output)
            {
                Node = node;
                Level = level;
                ReadSiblings = readSiblings;
                Output = output;
            }

            /// <summary>
            /// The node currently being read, this moves along the chain of siblings.
            /// </summary>
            public DictionaryToken Node { get; private set; }

            /// <summary>
            /// The level in the outline tree of every node in this chain.
            /// </summary>
            public int Level { get; }

            /// <summary>
            /// Whether the node this chain started from should be followed by its siblings.
            /// </summary>
            public bool ReadSiblings { get; }

            /// <summary>
            /// The list the bookmarks created for this chain are added to.
            /// </summary>
            public List<BookmarkNode> Output { get; }

            /// <summary>
            /// The bookmarks created for the children of <see cref="Node"/>.
            /// </summary>
            public List<BookmarkNode> Children { get; private set; } = new List<BookmarkNode>();

            /// <summary>
            /// The title of <see cref="Node"/>.
            /// </summary>
            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// Whether the children of <see cref="Node"/> have already been queued for reading.
            /// </summary>
            public bool ChildrenRead { get; set; }

            /// <summary>
            /// Whether <see cref="Node"/> is still the node this chain started from.
            /// </summary>
            public bool IsChainHead { get; set; } = true;

            /// <summary>
            /// Move on to the next node in the chain of siblings.
            /// </summary>
            public void MoveTo(DictionaryToken sibling)
            {
                Node = sibling;
                Children = new List<BookmarkNode>();
                Title = string.Empty;
                ChildrenRead = false;
            }
        }
    }
}
