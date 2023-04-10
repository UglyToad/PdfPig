namespace UglyToad.PdfPig.Outline
{
    using Actions;
    using Content;
    using Destinations;
    using Logging;
    using Parser.Parts;
    using System.Collections.Generic;
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
        public Bookmarks GetBookmarks(Catalog catalog)
        {
            if (!catalog.CatalogDictionary.TryGet(NameToken.Outlines, pdfScanner, out DictionaryToken outlinesDictionary))
            {
                return null;
            }

            if (outlinesDictionary.TryGet(NameToken.Type, pdfScanner, out NameToken typeName) && typeName != NameToken.Outlines)
            {
                log?.Error($"Outlines (bookmarks) dictionary did not have correct type specified: {typeName}.");
            }

            if (!outlinesDictionary.TryGet(NameToken.First, pdfScanner, out DictionaryToken next))
            {
                return null;
            }

            var roots = new List<BookmarkNode>();
            var seen = new HashSet<IndirectReference>();

            while (next != null)
            {
                ReadBookmarksRecursively(next, 0, false, seen, catalog.NamedDestinations, roots);

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
        /// Extract bookmarks recursively.
        /// </summary>
        private void ReadBookmarksRecursively(DictionaryToken nodeDictionary, int level, bool readSiblings, HashSet<IndirectReference> seen,
            NamedDestinations namedDestinations,
            List<BookmarkNode> list)
        {
            // 12.3 Document-Level Navigation

            // 12.3.3 Document Outline - Title
            // (Required) The text that shall be displayed on the screen for this item.
            if (!nodeDictionary.TryGetOptionalStringDirect(NameToken.Title, pdfScanner, out var title))
            {
                throw new PdfDocumentFormatException($"Invalid title for outline (bookmark) node: {nodeDictionary}.");
            }

            var children = new List<BookmarkNode>();
            if (nodeDictionary.TryGet(NameToken.First, pdfScanner, out DictionaryToken firstChild))
            {
                ReadBookmarksRecursively(firstChild, level + 1, true, seen, namedDestinations, children);
            }

            BookmarkNode bookmark;

            if (DestinationProvider.TryGetDestination(nodeDictionary, NameToken.Dest, namedDestinations, pdfScanner, log, false, out var destination))
            {
                bookmark = new DocumentBookmarkNode(title, level, destination, children);
            }
            else if (ActionProvider.TryGetAction(nodeDictionary, namedDestinations, pdfScanner, log, out var actionResult))
            {
                if (actionResult is GoToRAction goToRAction)
                {
                    bookmark = new ExternalBookmarkNode(title, level, goToRAction.Destination, children, goToRAction.Filename);
                }
                else if (actionResult is GoToAction goToAction)
                {
                    bookmark = new DocumentBookmarkNode(title, level, goToAction.Destination, children);
                }
                else if (actionResult is UriAction uriAction)
                {
                    bookmark = new UriBookmarkNode(title, level, uriAction.Uri, children);
                }
                else
                {
                    return;
                }
            }
            else
            {
                log.Error($"No /Dest(ination) or /A(ction) entry found for bookmark node: {nodeDictionary}.");
                return;
            }

            list.Add(bookmark);

            if (!readSiblings)
            {
                return;
            }

            // Walk all siblings if this was the first child.
            var current = nodeDictionary;
            while (true)
            {
                if (!current.TryGet(NameToken.Next, out IndirectReferenceToken nextReference)
                    || !seen.Add(nextReference.Data))
                {
                    break;
                }

                current = DirectObjectFinder.Get<DictionaryToken>(nextReference, pdfScanner);

                if (current == null)
                {
                    break;
                }

                ReadBookmarksRecursively(current, level, false, seen, namedDestinations, list);
            }
        }
    }
}
