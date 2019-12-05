namespace UglyToad.PdfPig.Outline
{
    using Content;
    using Destinations;
    using Exceptions;
    using Logging;
    using Parser.Parts;
    using System;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class BookmarksProvider
    {
        private readonly ILog log;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly bool isLenientParsing;

        public BookmarksProvider(ILog log, IPdfTokenScanner pdfScanner, bool isLenientParsing)
        {
            this.log = log;
            this.pdfScanner = pdfScanner;
            this.isLenientParsing = isLenientParsing;
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

            if (!isLenientParsing && outlinesDictionary.TryGet(NameToken.Type, pdfScanner, out NameToken typeName)
                && typeName != NameToken.Outlines)
            {
                throw new PdfDocumentFormatException($"Outlines (bookmarks) dictionary did not have correct type specified: {typeName}.");
            }

            if (!outlinesDictionary.TryGet(NameToken.First, pdfScanner, out DictionaryToken next))
            {
                return null;
            }

            var namedDestinations = ReadNamedDestinations(catalog, pdfScanner, isLenientParsing, log);

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

            if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out ArrayToken destArray))
            {
                var destination = GetExplicitDestination(destArray, catalog, isLenientParsing, log);

                bookmark = new DocumentBookmarkNode(title, level, destination, children);
            }
            else if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out IDataToken<string> destStringToken))
            {
                // 12.3.2.3 Named Destinations
                if (namedDestinations.TryGetValue(destStringToken.Data, out var destination))
                {
                    bookmark = new DocumentBookmarkNode(title, level, destination, children);
                }
                else if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Invalid destination name for bookmark node: {destStringToken.Data}.");
                }
                else
                {
                    return;
                }
            }
            else if (nodeDictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary)
            && TryGetAction(actionDictionary, catalog, pdfScanner, isLenientParsing, namedDestinations,
                         log, out var actionResult))
            {
                if (actionResult.isExternal)
                {
                    bookmark = new ExternalBookmarkNode(title, level, actionResult.externalFileName, children);
                }
                else if (actionResult.destination != null)
                {
                    bookmark = new DocumentBookmarkNode(title, level, actionResult.destination, children);
                }
                else if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Invalid action for bookmark node: {actionDictionary}.");
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

                ReadBookmarksRecursively(current, level, false, seen, namedDestinations, catalog, list);
            }
        }

        #region Named Destinations
        private static IReadOnlyDictionary<string, ExplicitDestination> ReadNamedDestinations(Catalog catalog, IPdfTokenScanner pdfScanner,
            bool isLenientParsing, ILog log)
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

                    if (TryReadExplicitDestination(value, catalog, pdfScanner, isLenientParsing, log, out var destination))
                    {
                        result[kvp.Key] = destination;
                    }
                    else if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Failed to find explicit destination for value '{value}' in: {dests}.");
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
                ExtractNameTree(dests, catalog, pdfScanner, isLenientParsing, log, result);
            }

            return result;
        }

        private static void ExtractNameTree(DictionaryToken nameTreeNodeDictionary, Catalog catalog, IPdfTokenScanner pdfScanner,
            bool isLenientParsing,
            ILog log,
            Dictionary<string, ExplicitDestination> explicitDestinations)
        {
            if (nameTreeNodeDictionary.TryGet(NameToken.Names, pdfScanner, out ArrayToken nodeNames))
            {
                for (var i = 0; i < nodeNames.Length; i += 2)
                {
                    if (!(nodeNames[i] is IDataToken<string> key))
                    {
                        if (isLenientParsing)
                        {
                            continue;
                        }

                        throw new PdfDocumentFormatException($"Invalid key '{nodeNames[i]}' in names tree for explicit destinations: {nameTreeNodeDictionary}.");
                    }

                    var value = nodeNames[i + 1];

                    if (TryReadExplicitDestination(value, catalog, pdfScanner, isLenientParsing, log, out var destination))
                    {
                        explicitDestinations[key.Data] = destination;
                    }
                    else if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Failed to find explicit destination for value '{value}' in: {nameTreeNodeDictionary}.");
                    }
                }
            }

            if (nameTreeNodeDictionary.TryGet(NameToken.Kids, pdfScanner, out ArrayToken kids))
            {
                foreach (var kid in kids.Data)
                {
                    if (DirectObjectFinder.TryGet(kid, pdfScanner, out DictionaryToken kidDictionary))
                    {
                        ExtractNameTree(kidDictionary, catalog, pdfScanner, isLenientParsing, log, explicitDestinations);
                    }
                    else if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Invalid kids entry in PDF name tree: {kid} in {kids}.");
                    }
                }
            }
        }

        private static bool TryReadExplicitDestination(IToken value, Catalog catalog, IPdfTokenScanner pdfScanner,
            bool isLenientParsing, ILog log, out ExplicitDestination destination)
        {
            if (DirectObjectFinder.TryGet(value, pdfScanner, out ArrayToken valueArray))
            {
                destination = GetExplicitDestination(valueArray, catalog, isLenientParsing, log);
                return true;
            }

            if (DirectObjectFinder.TryGet(value, pdfScanner, out DictionaryToken valueDictionary)
                     && valueDictionary.TryGet(NameToken.D, pdfScanner, out valueArray))
            {
                destination = GetExplicitDestination(valueArray, catalog, isLenientParsing, log);
                return true;
            }

            destination = null;
            return false;
        }

        private static ExplicitDestination GetExplicitDestination(ArrayToken explicitDestinationArray, Catalog catalog,
            bool isLenientParsing,
            ILog log)
        {
            if (explicitDestinationArray == null)
            {
                throw new ArgumentNullException(nameof(explicitDestinationArray));
            }

            if (explicitDestinationArray.Length == 0)
            {
                throw new ArgumentException("Invalid (empty) array for an explicit destination.", nameof(explicitDestinationArray));
            }

            var pageNumber = 1;

            var pageToken = explicitDestinationArray[0];
            if (pageToken is IndirectReferenceToken pageIndirectReferenceToken)
            {
                pageNumber = catalog.GetPageByReference(pageIndirectReferenceToken.Data).PageNumber ?? 1;
            }
            else if (pageToken is NumericToken pageNumericToken)
            {
                pageNumber = pageNumericToken.Int + 1;
            }
            else
            {
                var errorMessage = $"{nameof(GetExplicitDestination)} No page number given in 'Dest': '{explicitDestinationArray}'.";
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException(errorMessage);
                }

                log.Error(errorMessage);
            }

            var destTypeToken = explicitDestinationArray[1] as NameToken;
            if (destTypeToken == null)
            {
                var errorMessage = $"Missing name token as second argument to explicit destination: {explicitDestinationArray}.";
                if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException(errorMessage);
                }

                log.Error(errorMessage);

                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitPage, ExplicitDestinationCoordinates.Empty);
            }

            if (destTypeToken.Equals(NameToken.XYZ))
            {
                // [page /XYZ left top zoom]
                var left = explicitDestinationArray[2] as NumericToken;
                var top = explicitDestinationArray[3] as NumericToken;

                return new ExplicitDestination(pageNumber, ExplicitDestinationType.XyzCoordinates,
                    new ExplicitDestinationCoordinates(left?.Data, top?.Data));
            }

            if (destTypeToken.Equals(NameToken.Fit))
            {
                // [page /Fit]
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitPage,
                    ExplicitDestinationCoordinates.Empty);
            }

            if (destTypeToken.Equals(NameToken.FitH))
            {
                // [page /FitH top]
                var top = explicitDestinationArray[2] as NumericToken;
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitHorizontally,
                    new ExplicitDestinationCoordinates(null, top?.Data));
            }

            if (destTypeToken.Equals(NameToken.FitV))
            {
                // [page /FitV left]
                var left = explicitDestinationArray[2] as NumericToken;
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitVertically,
                    new ExplicitDestinationCoordinates(left?.Data));
            }

            if (destTypeToken.Equals(NameToken.FitR))
            {
                // [page /FitR left bottom right top]
                var left = explicitDestinationArray[2] as NumericToken;
                var bottom = explicitDestinationArray[3] as NumericToken;
                var right = explicitDestinationArray[4] as NumericToken;
                var top = explicitDestinationArray[5] as NumericToken;

                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitRectangle,
                    new ExplicitDestinationCoordinates(left?.Data, top?.Data, right?.Data, bottom?.Data));
            }

            if (destTypeToken.Equals(NameToken.FitB))
            {
                // [page /FitB]
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBox,
                    ExplicitDestinationCoordinates.Empty);
            }

            if (destTypeToken.Equals(NameToken.FitBH))
            {
                // [page /FitBH top]
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxHorizontally,
                    new ExplicitDestinationCoordinates(null, (explicitDestinationArray[2] as NumericToken)?.Data));
            }

            if (destTypeToken.Equals(NameToken.FitBV))
            {
                // [page /FitBV left]
                return new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxVertically,
                    new ExplicitDestinationCoordinates((explicitDestinationArray[2] as NumericToken)?.Data));
            }

            throw new PdfDocumentFormatException($"Unknown explicit destination type: {destTypeToken}.");
        }
        #endregion

        private static bool TryGetAction(DictionaryToken actionDictionary, Catalog catalog, IPdfTokenScanner pdfScanner,
            bool isLenientParsing,
            IReadOnlyDictionary<string, ExplicitDestination> namedDestinations,
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
                if (actionDictionary.TryGet(NameToken.D, pdfScanner, out ArrayToken destinationArray))
                {
                    var destination = GetExplicitDestination(destinationArray, catalog, isLenientParsing, log);

                    result = (false, null, destination);

                    return true;
                }
                else if (actionDictionary.TryGet(NameToken.D, pdfScanner, out IDataToken<string> destinationName)
                         && namedDestinations.TryGetValue(destinationName.Data, out var destination))
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
