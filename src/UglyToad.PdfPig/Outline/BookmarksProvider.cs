namespace UglyToad.PdfPig.Outline
{
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
                ReadBookmarksRecursively(next, 0, false, seen, catalog, roots);

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
            Catalog catalog,
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
                ReadBookmarksRecursively(firstChild, level + 1, true, seen, catalog, children);
            }

            BookmarkNode bookmark;

            if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out ArrayToken destArray)
                && catalog.NamedDestinations.TryGetExplicitDestination(destArray, pdfScanner, log, out var destination))
            {
                bookmark = new DocumentBookmarkNode(title, level, destination, children);
            }
            else if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out IDataToken<string> destStringToken))
            {
                // 12.3.2.3 Named Destinations
                if (catalog.NamedDestinations.TryGet(destStringToken.Data, out destination))
                {
                    bookmark = new DocumentBookmarkNode(title, level, destination, children);
                }
                else
                {
                    return;
                }
            }
            else if (nodeDictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary)
                && TryGetAction(actionDictionary, catalog, pdfScanner, log, out var actionResult))
            {
                if (actionResult.isExternal)
                {
                    bookmark = new ExternalBookmarkNode(title, level, actionResult.externalFileName, children);
                }
                else if (actionResult.destination != null)
                {
                    bookmark = new DocumentBookmarkNode(title, level, actionResult.destination, children);
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

                ReadBookmarksRecursively(current, level, false, seen, catalog, list);
            }
        }

        

        private static bool TryGetAction(DictionaryToken actionDictionary, Catalog catalog, IPdfTokenScanner pdfScanner,
            ILog log,
            out (bool isExternal, string externalFileName, ExplicitDestination destination) result)
        {
            result = (false, null, null);

            if (!actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken actionType))
            {
                throw new PdfDocumentFormatException($"No action type (/S) specified for action: {actionDictionary}.");
            }

            if (actionType.Equals(NameToken.GoTo))
            {
                if (actionDictionary.TryGet(NameToken.D, pdfScanner, out ArrayToken destinationArray)
                && NamedDestinationsProvider.TryGetExplicitDestination(destinationArray, catalog.Pages, log, out var destination))
                {
                    result = (false, null, destination);

                    return true;
                }

                if (actionDictionary.TryGet(NameToken.D, pdfScanner, out IDataToken<string> destinationName)
                         && catalog.NamedDestinations.TryGet(destinationName.Data, out destination))
                {
                    result = (false, null, destination);

                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToR))
            {
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var filename))
                {
                    result = (true, filename, null);
                    return true;
                }

                result = (true, string.Empty, null);
                return true;
            }

            return false;
        }
    }
}
