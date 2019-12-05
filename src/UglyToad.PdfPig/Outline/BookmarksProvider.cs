using System;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Outline
{
    using System.Collections.Generic;
    using Content;
    using Exceptions;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Util;

    internal class ExplicitDestination
    {
        public int? PageNumber { get; }

        public ExplicitDestinationType Type { get; }

        public ExplicitDestinationCoordinates Coordinates { get; }

        public ExplicitDestination(int? pageNumber,
            ExplicitDestinationType type,
            ExplicitDestinationCoordinates coordinates)
        {
            PageNumber = pageNumber;
            Type = type;
            Coordinates = coordinates;
        }
    }

    /// <summary>
    /// The display type for opening an <see cref="ExplicitDestination"/>.
    /// </summary>
    internal enum ExplicitDestinationType
    {
        /// <summary>
        /// Display the page with the given top left coordinates and 
        /// zoom level.
        /// </summary>
        XyzCoordinates = 0,
        /// <summary>
        /// Fit the entire page within the window.
        /// </summary>
        FitPage = 1,
        /// <summary>
        /// Fit the entire page width within the window.
        /// </summary>
        FitHorizontally = 2,
        /// <summary>
        /// Fit the entire page height within the window.
        /// </summary>
        FitVertically = 3,
        /// <summary>
        /// Fit the rectangle specified by the <see cref="ExplicitDestinationCoordinates"/>
        /// within the window.
        /// </summary>
        FitRectangle = 4,
        /// <summary>
        /// Fit the page's bounding box within the window.
        /// </summary>
        FitBoundingBox = 5,
        /// <summary>
        /// Fit the page's bounding box width within the window.
        /// </summary>
        FitBoundingBoxHorizontally = 6,
        /// <summary>
        /// Fit the page's bounding box height within the window.
        /// </summary>
        FitBoundingBoxVertically = 7
    }

    /// <summary>
    /// The coordinates of the region to display for a <see cref="ExplicitDestination"/>.
    /// </summary>
    internal class ExplicitDestinationCoordinates
    {
        public static ExplicitDestinationCoordinates Empty { get; } = new ExplicitDestinationCoordinates(null, null, null, null);
        /// <summary>
        /// The left side of the region to display.
        /// </summary>
        public decimal? Left { get; }

        /// <summary>
        /// The top edge of the region to display.
        /// </summary>
        public decimal? Top { get; }

        /// <summary>
        /// The right side of the region to display
        /// </summary>
        public decimal? Right { get; }

        /// <summary>
        /// The bottom edge of the region to display.
        /// </summary>
        public decimal? Bottom { get; }

        public ExplicitDestinationCoordinates(decimal? left)
        {
            Left = left;
        }

        public ExplicitDestinationCoordinates(decimal? left, decimal? top)
        {
            Left = left;
            Top = top;
        }

        public ExplicitDestinationCoordinates(decimal? left, decimal? top, decimal? right, decimal? bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

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

            return null;
        }

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
                    var key = nodeNames[i] as IDataToken<string>;

                    if (key == null)
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

            if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out ArrayToken destArray))
            {
                var desti = GetExplicitDestination(destArray, catalog, isLenientParsing, log);
            }
            else if (nodeDictionary.TryGet(NameToken.Dest, pdfScanner, out IDataToken<string> destStringToken))
            {
                // 12.3.2.3 Named Destinations
                if (namedDestinations.TryGetValue(destStringToken.Data, out var destination))
                {
                    
                }
                else if (!isLenientParsing)
                {
                    throw new PdfDocumentFormatException($"Invalid destination name for bookmark node: {destStringToken.Data}.");
                }
            }

            var children = new List<BookmarkNode>();
            if (nodeDictionary.TryGet(NameToken.First, pdfScanner, out DictionaryToken firstChild))
            {
                ReadBookmarksRecursively(firstChild, level + 1, true, seen, namedDestinations, catalog, children);
            }

                list.Add(new BookmarkNode(title, PdfPoint.Origin, new PdfRectangle(), level, 1, string.Empty, false, children));

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

            //// 12.3.2 Destinations
            //if (nodeDictionary.TryGet(NameToken.Dest, out ArrayToken destToken))
            //{
            //    // 12.3.2.2 Explicit Destinations
            //    GetDestination(destToken, newNode);
            //}
            //else if (dictionary.TryGet(NameToken.Dest, out IDataToken<string> destStringToken))
            //{
            //    // 12.3.2.3 Named Destinations
            //    GetNamedDestination(destStringToken, ref newNode);
            //}
            //else if (dictionary.TryGet(NameToken.A, out IToken actionToken))
            //{
            //    // 12.6 Actions
            //    GetActions(actionToken, ref newNode);
            //}
            //else
            //{
            //    log.Error("BookmarksProvider.RecursiveBookmark(): No 'Dest' or 'Action' token found.");
            //}
        }


        private static int ParsePageNumber(string goToStr)
        {
            if (int.TryParse(System.Text.RegularExpressions.Regex.Match(goToStr, "[0-9]+").Value, out int number))
            {
                return number + 1;
            }
            return 0;
        }

        //#region Destinations
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

            var pageNumber = default(int?);

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

        //private void GetNamedDestination(IDataToken<string> destStringToken, ref BookmarkNode currentNode)
        //{
        //    if (destStringToken == null)
        //    {
        //        throw new ArgumentNullException(nameof(destStringToken), "BookmarksProvider.GetNamedDestination()");
        //    }

        //    // 12.3.2.3 Named Destinations
        //    if (structure.Catalog.CatalogDictionary.TryGet(NameToken.Dests, out IndirectReferenceToken destsToken11))
        //    {
        //        // In PDF 1.1, the correspondence between name objects and destinations shall be defined by the 
        //        // Dests entry in the document catalogue (see 7.7.2, “Document Catalog”). The value of this entry 
        //        // shall be a dictionary in which each key is a destination name and the corresponding value is 
        //        // either an array defining the destination, using the syntax shown in Table 151, or a dictionary
        //        // with a D entry whose value is such an array.
        //        throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): PDF 1.1.");
        //    }
        //    else if (structure.Catalog.CatalogDictionary.TryGet(NameToken.Names, out IndirectReferenceToken namesToken))
        //    {
        //        // In PDF 1.2 and later, the correspondence between strings and destinations may alternatively be
        //        // defined by the Dests entry in the document’s name dictionary (see 7.7.4, “Name Dictionary”). 
        //        // The value of this entry shall be a name tree (7.9.6, “Name Trees”) mapping name strings to 
        //        // destinations. (The keys in the name tree may be treated as text strings for display purposes.) 
        //        // The destination value associated with a key in the name tree may be either an array or a 
        //        // dictionary, as described in the preceding paragraph.
        //        var namesDictionary = structure.GetObject(namesToken.Data).Data as DictionaryToken;
        //        if (namesDictionary == null)
        //        {
        //            throw new ArgumentNullException(nameof(namesDictionary), "BookmarksProvider.GetNamedDestination()");
        //        }

        //        if (namesDictionary.TryGet(NameToken.Dests, out IndirectReferenceToken destsToken))
        //        {
        //            var destsDictionary = structure.GetObject(destsToken.Data).Data as DictionaryToken;
        //            if (destsDictionary == null)
        //            {
        //                throw new ArgumentNullException(nameof(destsDictionary), "BookmarksProvider.GetNamedDestination()");
        //            }

        //            IToken found = FindInNameTree(destStringToken, destsDictionary);
        //            if (found != null)
        //            {
        //                ArrayToken destToken = null;
        //                if (found is IndirectReferenceToken indirect)
        //                {
        //                    var pageObject = structure.GetObject(indirect.Data);
        //                    if (pageObject.Data is DictionaryToken dictionaryToken)
        //                    {
        //                        if (!dictionaryToken.TryGet(NameToken.D, out destToken))
        //                        {
        //                            throw new ArgumentException("BookmarksProvider.GetNamedDestination(): Cannot find token 'D'.");
        //                        }
        //                    }
        //                    else if (pageObject.Data is ArrayToken arrayToken)
        //                    {
        //                        destToken = arrayToken;
        //                    }
        //                    else
        //                    {
        //                        throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type '" + pageObject.Data + "'.");
        //                    }
        //                }
        //                else if (found is ArrayToken arrayToken)
        //                {
        //                    destToken = arrayToken;
        //                }
        //                else if (found is DictionaryToken)
        //                {
        //                    throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type 'DictionaryToken'.");
        //                }
        //                else
        //                {
        //                    throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type '" + found.GetType() + "'.");
        //                }

        //                var pageNumber = structure.Catalog.GetPageByReference(((IndirectReferenceToken)destToken[0]).Data).PageNumber;
        //                if (pageNumber.HasValue)
        //                {
        //                    currentNode.PageNumber = pageNumber.Value;
        //                }
        //                GetDestination(destToken, currentNode);
        //            }
        //        }
        //    }
        //}

        //private IToken FindInNameTree<T>(T find, DictionaryToken dictionaryToken) where T : IDataToken<string>
        //{
        //    // 7.9.6 Name Trees
        //    // Intermediate node
        //    if (dictionaryToken.TryGet(NameToken.Kids, out ArrayToken kidsToken))
        //    {
        //        foreach (var kid in kidsToken.Data)
        //        {
        //            var dictionary = structure.GetObject(((IndirectReferenceToken)kid).Data).Data as DictionaryToken;
        //            if (dictionary != null && dictionary.TryGet(NameToken.Limits, out ArrayToken limits))
        //            {
        //                // (Intermediate and leaf nodes only; required) Shall be an array of two strings,
        //                // that shall specify the (lexically) least and greatest keys included in the
        //                // Names array of a leaf node or in the Names arrays of any leaf nodes that are
        //                // descendants of an intermediate node.
        //                var least = limits[0] as IDataToken<string>;
        //                var greatest = limits[1] as IDataToken<string>;

        //                if (IsStringBetween(find.Data, least.Data, greatest.Data))
        //                {
        //                    var indRef = FindInNameTree(find, dictionary);
        //                    if (indRef != null)
        //                    {
        //                        return indRef;
        //                    }
        //                    else
        //                    {
        //                        throw new ArgumentException("BookmarksProvider.FindNamedDestination(): Did no find the key '" + find.Data + "' in Name Tree.");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // Leaf node
        //        if (dictionaryToken.TryGet(NameToken.Names, out ArrayToken names))
        //        {
        //            // Names
        //            // Shall be an array of the form [key_1, value_1, key_2, value_2, …, key_n, value_n]
        //            // where each key_i shall be a string and the corresponding value_i shall be the object 
        //            // associated with that key. The keys shall be sorted in lexical order, as described below.
        //            for (int i = 0; i < names.Length; i += 2)
        //            {
        //                if (names[i] is IDataToken<string> n && n.Data.Equals(find.Data))
        //                {
        //                    return names[i + 1];
        //                }
        //            }
        //        }
        //        else
        //        {
        //            throw new ArgumentNullException("BookmarksProvider.FindNamedDestination(): Could not find ArrayToken 'Names' in dictionary.");
        //        }
        //    }
        //    throw new ArgumentException("BookmarksProvider.FindNamedDestination(): Did no find the key '" + find.Data + "' in Name Tree.");
        //}

        //private bool IsStringBetween(string str, string least, string greatest)
        //{
        //    return (string.Compare(str, least, StringComparison.Ordinal) >= 0 &&
        //            string.Compare(str, greatest, StringComparison.Ordinal) <= 0);
        //}
        //#endregion

        //#region Actions
        //private void GetActions(IToken actionToken, ref BookmarkNode currentNode)
        //{
        //    if (actionToken is DictionaryToken dictionaryToken)
        //    {
        //        if (dictionaryToken.TryGet(NameToken.S, out NameToken sToken))
        //        {
        //            if (sToken.Equals(NameToken.GoTo)) // 12.6.4.2, Go-To Actions
        //            {
        //                if (dictionaryToken.TryGet(NameToken.D, out IToken goToToken))
        //                {
        //                    HandleGoToAction(goToToken, ref currentNode);
        //                }
        //                else
        //                {
        //                    throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'D' in 'GoTo'.");
        //                }
        //            }
        //            else if (sToken.Equals(NameToken.GoToR)) // 12.6.4.3, Remote Go-To Actions
        //            {
        //                if (dictionaryToken.TryGet(NameToken.D, out IToken goToRToken))
        //                {
        //                    if (dictionaryToken.TryGet(NameToken.F, out IToken remoteFileToken))
        //                    {
        //                        currentNode.ExternalLink = GetString(NameToken.F, remoteFileToken);
        //                    }
        //                    HandleGoToRAction(goToRToken, ref currentNode);
        //                }
        //                else
        //                {
        //                    throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'D' in 'GoToR'.");
        //                }
        //            }
        //            else
        //            {
        //                currentNode.IsExternal = true;
        //                log.Debug("BookmarksProvider.GetActions(): Ignoring unknown token '" + sToken.Data + "'.");
        //            }
        //        }
        //        else
        //        {
        //            throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'S' in 'Action'.");
        //        }
        //    }
        //    else if (actionToken is IndirectReferenceToken indirectReferenceToken)
        //    {
        //        var tempToken = structure.GetObject(indirectReferenceToken.Data).Data;
        //        if (tempToken is DictionaryToken dictionaryAction)
        //        {
        //            GetActions(dictionaryAction, ref currentNode);
        //        }
        //        else
        //        {
        //            throw new NotImplementedException("BookmarksProvider.GetActions(): " + nameof(tempToken) + " of type " + tempToken.GetType() + ".");
        //        }
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("BookmarksProvider.GetActions(): " + nameof(actionToken) + " of type " + actionToken.GetType() + ".");
        //    }
        //}

        //private void HandleGoToRAction(IToken goToRToken, ref BookmarkNode currentNode)
        //{
        //    currentNode.IsExternal = true;
        //    HandleGoToAction(goToRToken, ref currentNode);
        //}

        //private void HandleGoToAction(IToken goToToken, ref BookmarkNode currentNode)
        //{
        //    if (goToToken is ArrayToken arrayToken)
        //    {
        //        GetDestination(arrayToken, currentNode);
        //    }
        //    else if (goToToken is IDataToken<string> stringToken)
        //    {
        //        GetNamedDestination(stringToken, ref currentNode);
        //        if (currentNode.PageNumber == 0)
        //        {
        //            currentNode.PageNumber = ParsePageNumber(stringToken.Data);
        //        }
        //    }
        //    else if (goToToken is IndirectReferenceToken indirectReferenceToken)
        //    {
        //        HandleGoToAction(structure.GetObject(indirectReferenceToken.Data).Data, ref currentNode);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("BookmarksProvider.HandleGoToAction(): " + nameof(goToToken) + " of type " + goToToken.GetType());
        //    }
        //}
        //#endregion
    }
}
