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

            if (TryGetDestination(nodeDictionary, NameToken.Dest, namedDestinations, pdfScanner, log, false, out var destination))
            {
                bookmark = new DocumentBookmarkNode(title, level, destination, children);
            }
            else if (TryGetAction(nodeDictionary, namedDestinations, pdfScanner, log, out var actionResult))
            {
                if (actionResult.isExternal)
                {
                    bookmark = new ExternalBookmarkNode(title, level, actionResult.destination, actionResult.externalFileName, children);
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

                ReadBookmarksRecursively(current, level, false, seen, namedDestinations, list);
            }
        }

        /// <summary>
        /// Get explicit destination or a named destination (Ref 12.3.2.3) from dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="destinationToken">Token name, can be D or Dest</param>
        /// <param name="namedDestinations"></param>
        /// <param name="pdfScanner"></param>
        /// <param name="log"></param>
        /// <param name="isRemoteDestination">in case we are looking up a destination for a GoToR (Go To Remote) action: pass in true
        /// to enforce a check for indirect page references (which is not allowed for GoToR)</param>
        /// <param name="destination"></param>
        /// <returns></returns>
        internal static bool TryGetDestination(DictionaryToken dictionary, NameToken destinationToken, NamedDestinations namedDestinations, IPdfTokenScanner pdfScanner, ILog log, bool isRemoteDestination, out ExplicitDestination destination)
        {
            if (dictionary.TryGet(destinationToken, pdfScanner, out ArrayToken destArray))
            {
                return namedDestinations.TryGetExplicitDestination(destArray, log, isRemoteDestination, out destination);
            }
            if (dictionary.TryGet(destinationToken, pdfScanner, out IDataToken<string> destStringToken))
            {
                return namedDestinations.TryGet(destStringToken.Data, out destination);
            }
            destination = null;
            return false;
        }

        /// <summary>
        /// Get an action (A) from dictionary. If GoTo, GoToR or GoToE, also fetches the action destination.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="namedDestinations"></param>
        /// <param name="pdfScanner"></param>
        /// <param name="log"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        /// <exception cref="PdfDocumentFormatException"></exception>
        internal static bool TryGetAction(DictionaryToken dictionary, NamedDestinations namedDestinations, IPdfTokenScanner pdfScanner,
            ILog log, out (bool isExternal, string externalFileName, ExplicitDestination destination) result)
        {
            result = (false, null, null);

            if (!dictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary))
            {
                return false;
            }

            if (!actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken actionType))
            {
                throw new PdfDocumentFormatException($"No action type (/S) specified for action: {actionDictionary}.");
            }

            if (actionType.Equals(NameToken.GoTo))
            {
                // For GoTo, D(estination) is required
                if (TryGetDestination(actionDictionary, NameToken.D, namedDestinations, pdfScanner, log, false, out var destination))
                {
                    result = (false, null, destination);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToR))
            {
                // For GoToR, F(ile) and D(estination) are required
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var filename)
                    && TryGetDestination(actionDictionary, NameToken.D, namedDestinations, pdfScanner, log, true, out var destination))
                {
                    result = (true, filename, destination);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToE))
            {
                // For GoToE, D(estination) is required
                if (TryGetDestination(actionDictionary, NameToken.D, namedDestinations, pdfScanner, log, true, out var destination))
                {
                    // F(ile specification) is optional
                    if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var fileSpecification))
                    {
                        fileSpecification = null;
                    }
                    result = (true, fileSpecification, destination);
                    return true;
                }
            }

            return false;
        }
    }
}
