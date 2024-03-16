﻿namespace UglyToad.PdfPig.Outline.Destinations
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Logging;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class NamedDestinationsProvider
    {
        internal static NamedDestinations Read(DictionaryToken catalogDictionary, IPdfTokenScanner pdfScanner, Pages pages, ILog? log)
        {
            var destinationsByName = new Dictionary<string, ExplicitDestination>();

            if (catalogDictionary.TryGet(NameToken.Dests, pdfScanner, out DictionaryToken? destinations))
            {
                /*
                 * In PDF 1.1, the correspondence between name objects and destinations is defined by the /Dests entry in the document catalog.
                 * The value of this entry is a dictionary in which each key is a destination name and the corresponding value is either an array
                 * defining the destination, using the explicit destination syntax, or a dictionary with a /D entry whose value is such an array. 
                 */
                foreach (var kvp in destinations.Data)
                {
                    var value = kvp.Value;

                    if (TryReadExplicitDestination(value, pdfScanner, pages, log, false, out var destination))
                    {
                        destinationsByName[kvp.Key] = destination;
                    }
                }
            }
            else if (catalogDictionary.TryGet(NameToken.Names, pdfScanner, out DictionaryToken? names)
                     && names.TryGet(NameToken.Dests, pdfScanner, out destinations))
            {
                /*
                 * In PDF 1.2, the correspondence between strings and destinations is defined by the /Dests entry in the document's name dictionary.
                 * The value of the /Dests entry is a name tree mapping name strings to destinations.
                 * The keys in the name tree may be treated as text strings for display purposes.
                 * The destination value associated with a key in the name tree may be either an array or a dictionary. 
                 */
                NameTreeParser.FlattenNameTree(destinations, pdfScanner, value =>
                {
                    if (TryReadExplicitDestination(value, pdfScanner, pages, log, false, out var destination))
                    {
                        return destination;
                    }

                    return null;
                }, destinationsByName!);
            }

            return new NamedDestinations(destinationsByName, pages);
        }

        private static bool TryReadExplicitDestination(
            IToken value,
            IPdfTokenScanner pdfScanner,
            Pages pages,
            ILog? log,
            bool isRemoteDestination,
            [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            destination = null;

            if (DirectObjectFinder.TryGet(value, pdfScanner, out ArrayToken? valueArray)
                && TryGetExplicitDestination(valueArray, pages, log, isRemoteDestination, out destination))
            {
                return true;
            }

            if (DirectObjectFinder.TryGet(value, pdfScanner, out DictionaryToken? valueDictionary)
                && valueDictionary.TryGet(NameToken.D, pdfScanner, out valueArray)
                && TryGetExplicitDestination(valueArray, pages, log, isRemoteDestination, out destination))
            {
                return true;
            }

            return false;
        }

        internal static bool TryGetExplicitDestination(
            ArrayToken explicitDestinationArray,
            Pages pages,
            ILog? log,
            bool isRemoteDestination, 
            [NotNullWhen(true)] out ExplicitDestination? destination)
        {
            destination = null;

            if (explicitDestinationArray is null || explicitDestinationArray.Length == 0)
            {
                return false;
            }

            double? GetPossibleEntry(int index)
            {
                if (index >= explicitDestinationArray.Length)
                {
                    return null;
                }

                if (explicitDestinationArray[index] is NumericToken num)
                {
                    return num.Data;
                }

                return null;
            }

            int pageNumber;

            var pageToken = explicitDestinationArray[0];

            if (pageToken is IndirectReferenceToken pageIndirectReferenceToken)
            {
                if (isRemoteDestination)
                {
                    // Table 8.50 Remote Go-To Actions
                    var errorMessage = $"{nameof(TryGetExplicitDestination)} Cannot use indirect reference for remote destination.";
                    log?.Error(errorMessage);
                    return false;
                }
                var page = pages.GetPageByReference(pageIndirectReferenceToken.Data);
                if (page?.PageNumber is null)
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

                log?.Error(errorMessage);

                return false;
            }

            NameToken? destTypeToken = null;
            if (explicitDestinationArray.Length > 1)
            {
                destTypeToken = explicitDestinationArray[1] as NameToken;
            }
            if (destTypeToken is null)
            {
                var errorMessage = $"Missing name token as second argument to explicit destination: {explicitDestinationArray}.";

                log?.Error(errorMessage);

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitPage, ExplicitDestinationCoordinates.Empty);

                return true;
            }

            if (destTypeToken.Equals(NameToken.XYZ))
            {
                // [page /XYZ left top zoom]
                var left = GetPossibleEntry(2);
                var top = GetPossibleEntry(3);

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.XyzCoordinates,
                    new ExplicitDestinationCoordinates(left, top));

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
                var top = GetPossibleEntry(2);
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitHorizontally,
                    new ExplicitDestinationCoordinates(null, top));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitV))
            {
                // [page /FitV left]
                var left = GetPossibleEntry(2);
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitVertically,
                    new ExplicitDestinationCoordinates(left));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitR))
            {
                // [page /FitR left bottom right top]
                var left = GetPossibleEntry(2);
                var bottom = GetPossibleEntry(3);
                var right = GetPossibleEntry(4);
                var top = GetPossibleEntry(5);

                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitRectangle,
                    new ExplicitDestinationCoordinates(left, top, right, bottom));

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
                var top = GetPossibleEntry(2);
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxHorizontally,
                    new ExplicitDestinationCoordinates(null, top));

                return true;
            }

            if (destTypeToken.Equals(NameToken.FitBV))
            {
                // [page /FitBV left]
                var left = GetPossibleEntry(2);
                destination = new ExplicitDestination(pageNumber, ExplicitDestinationType.FitBoundingBoxVertically,
                    new ExplicitDestinationCoordinates(left));

                return true;
            }

            return false;
        }
    }
}
