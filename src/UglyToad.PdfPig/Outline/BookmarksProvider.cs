﻿namespace UglyToad.PdfPig.Outline
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

            var namedDestinations = ReadNamedDestinations(catalog, pdfScanner, log);

            var roots = new List<BookmarkNode>();
            var seen = new HashSet<IndirectReference>();

            while (next != null)
            {
                ReadBookmarksRecursively(next, 0, false, seen, namedDestinations, catalog, roots);

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
            IReadOnlyDictionary<string, ExplicitDestination> namedDestinations,
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
                ReadBookmarksRecursively(firstChild, level + 1, true, seen, namedDestinations, catalog, children);
            }

            BookmarkNode bookmark;

            if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out ArrayToken destArray)
                && TryGetExplicitDestination(destArray, catalog, log, out var destination))
            {
                bookmark = new DocumentBookmarkNode(title, level, destination, children);
            }
            else if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out IDataToken<string> destStringToken))
            {
                // 12.3.2.3 Named Destinations
                if (namedDestinations.TryGetValue(destStringToken.Data, out destination))
                {
                    bookmark = new DocumentBookmarkNode(title, level, destination, children);
                }
                else
                {
                    return;
                }
            }
            else if (nodeDictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary)
                && TryGetAction(actionDictionary, catalog, pdfScanner, namedDestinations, log, title, level, children, out var actionResult))
            {
                bookmark = actionResult;
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

                ReadBookmarksRecursively(current, level, false, seen, namedDestinations, catalog, list);
            }
        }

        #region Named Destinations
        private static IReadOnlyDictionary<string, ExplicitDestination> ReadNamedDestinations(Catalog catalog, IPdfTokenScanner pdfScanner,
            ILog log)
        {
            var result = new Dictionary<string, ExplicitDestination>();

            if (catalog.CatalogDictionary.TryGet(NameToken.Dests, pdfScanner, out DictionaryToken dests))
            {
                /*
                 * In PDF 1.1, the correspondence between name objects and destinations is defined by the /Dests entry in the document catalog.
                 * The value of this entry is a dictionary in which each key is a destination name and the corresponding value is either an array
                 * defining the destination, using the explicit destination syntax, or a dictionary with a /D entry whose value is such an array. 
                 */
                foreach (var kvp in dests.Data)
                {
                    var value = kvp.Value;

                    if (TryReadExplicitDestination(value, catalog, pdfScanner, log, out var destination))
                    {
                        result[kvp.Key] = destination;
                    }
                }
            }
            else if (catalog.CatalogDictionary.TryGet(NameToken.Names, pdfScanner, out DictionaryToken names)
                     && names.TryGet(NameToken.Dests, pdfScanner, out dests))
            {
                /*
                 * In PDF 1.2, the correspondence between strings and destinations is defined by the /Dests entry in the document's name dictionary.
                 * The value of the /Dests entry is a name tree mapping name strings to destinations.
                 * The keys in the name tree may be treated as text strings for display purposes.
                 * The destination value associated with a key in the name tree may be either an array or a dictionary. 
                 */
                NameTreeParser.FlattenNameTree(dests, pdfScanner, value =>
                {
                    if (TryReadExplicitDestination(value, catalog, pdfScanner, log, out var destination))
                    {
                        return destination;
                    }

                    return null;
                }, result);
            }

            return result;
        }

        private static bool TryReadExplicitDestination(IToken value, Catalog catalog, IPdfTokenScanner pdfScanner,
            ILog log, out ExplicitDestination destination)
        {
            destination = null;

            if (DirectObjectFinder.TryGet(value, pdfScanner, out ArrayToken valueArray)
            && TryGetExplicitDestination(valueArray, catalog, log, out destination))
            {
                return true;
            }

            if (DirectObjectFinder.TryGet(value, pdfScanner, out DictionaryToken valueDictionary)
                && valueDictionary.TryGet(NameToken.D, pdfScanner, out valueArray)
                && TryGetExplicitDestination(valueArray, catalog, log, out destination))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetExplicitDestination(ArrayToken explicitDestinationArray, Catalog catalog,
            ILog log,
            out ExplicitDestination destination)
        {
            destination = null;

            if (explicitDestinationArray == null || explicitDestinationArray.Length == 0)
            {
                return false;
            }

            int pageNumber;

            var pageToken = explicitDestinationArray[0];

            if (pageToken is IndirectReferenceToken pageIndirectReferenceToken)
            {
                var page = catalog.GetPageByReference(pageIndirectReferenceToken.Data);

                if (page?.PageNumber == null)
                {
                    return false;
                }

                pageNumber = page.PageNumber.Value;
            }
            else if (pageToken is NumericToken pageNumericToken)
            {
                pageNumber = pageNumericToken.Int + 1;
            }
            else
            {
                var errorMessage = $"{nameof(TryGetExplicitDestination)} No page number given in 'Dest': '{explicitDestinationArray}'.";

                log.Error(errorMessage);

                return false;
            }

            var destTypeToken = explicitDestinationArray[1] as NameToken;
            if (destTypeToken == null)
            {
                var errorMessage = $"Missing name token as second argument to explicit destination: {explicitDestinationArray}.";

                log.Error(errorMessage);

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitPage, ExplicitDestinationCoordinates.Empty);

                return true;
            }

            if (destTypeToken.Equals(NameToken.XYZ))
            {
                // [page /XYZ left top zoom]
                var left = explicitDestinationArray[2] as NumericToken;
                var top = explicitDestinationArray[3] as NumericToken;

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.XyzCoordinates,
                    new ExplicitDestinationCoordinates(left?.Data, top?.Data));

                return true;
            }

            if (destTypeToken.Equals(NameToken.Fit))
            {
                // [page /Fit]
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitPage,
                    ExplicitDestinationCoordinates.Empty);

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitH))
            {
                // [page /FitH top]
                var top = explicitDestinationArray[2] as NumericToken;
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitHorizontally,
                    new ExplicitDestinationCoordinates(null, top?.Data));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitV))
            {
                // [page /FitV left]
                var left = explicitDestinationArray[2] as NumericToken;
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitVertically,
                    new ExplicitDestinationCoordinates(left?.Data));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitR))
            {
                // [page /FitR left bottom right top]
                var left = explicitDestinationArray[2] as NumericToken;
                var bottom = explicitDestinationArray[3] as NumericToken;
                var right = explicitDestinationArray[4] as NumericToken;
                var top = explicitDestinationArray[5] as NumericToken;

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitRectangle,
                    new ExplicitDestinationCoordinates(left?.Data, top?.Data, right?.Data, bottom?.Data));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitB))
            {
                // [page /FitB]
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBox,
                    ExplicitDestinationCoordinates.Empty);

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitBH))
            {
                // [page /FitBH top]
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxHorizontally,
                    new ExplicitDestinationCoordinates(null, (explicitDestinationArray[2] as NumericToken)?.Data));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitBV))
            {
                // [page /FitBV left]
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxVertically,
                    new ExplicitDestinationCoordinates((explicitDestinationArray[2] as NumericToken)?.Data));

                return true;
            }

            return false;
        }
        #endregion

        private static bool TryGetAction(DictionaryToken actionDictionary, Catalog catalog, IPdfTokenScanner pdfScanner,
            IReadOnlyDictionary<string, ExplicitDestination> namedDestinations,
            ILog log, string title, int level, List<BookmarkNode> children, out BookmarkNode result)
        {
            result = null;

            if (!actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken actionType))
            {
                throw new PdfDocumentFormatException($"No action type (/S) specified for action: {actionDictionary}.");
            }

            if (actionType.Equals(NameToken.GoTo))
            {
                if (actionDictionary.TryGet(NameToken.D, pdfScanner, out ArrayToken destinationArray)
                && TryGetExplicitDestination(destinationArray, catalog, log, out var destination))
                {
                    result = new DocumentBookmarkNode(title, level, destination, children);

                    return true;
                }

                if (actionDictionary.TryGet(NameToken.D, pdfScanner, out IDataToken<string> destinationName)
                         && namedDestinations.TryGetValue(destinationName.Data, out destination))
                {
                    result = new DocumentBookmarkNode(title, level, destination, children);

                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToR))
            {
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var filename))
                {
                    result = new ExternalBookmarkNode(title, level, filename, children);
                    return true;
                }

                result = new ExternalBookmarkNode(title, level, string.Empty, children);
                return true;
            }
            else if (actionType.Equals(NameToken.Uri))
            {
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.Uri, pdfScanner, out var uri))
                {
                    result = new UriBookmarkNode(title, level, uri, children);
                    return true;
                }

                result = new UriBookmarkNode(title, level, string.Empty, children);
                return true;
            }

            return false;
        }
    }
}
