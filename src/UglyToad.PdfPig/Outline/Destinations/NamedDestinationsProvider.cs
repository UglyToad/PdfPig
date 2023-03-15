namespace UglyToad.PdfPig.Outline;

using Content;
using Destinations;
using Logging;
using Parser.Parts;
using System.Collections.Generic;
using Tokenization.Scanner;
using Tokens;

internal static class NamedDestinationsProvider
{
    internal static NamedDestinations Read(DictionaryToken catalogDictionary, IPdfTokenScanner pdfScanner, Pages pages, ILog log) 
    {
        var destinationsByName = new Dictionary<string, ExplicitDestination>();

        if (catalogDictionary.TryGet(NameToken.Dests, pdfScanner, out DictionaryToken destinations))
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
        else if (catalogDictionary.TryGet(NameToken.Names, pdfScanner, out DictionaryToken names)
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
            }, destinationsByName);
        }

        return new NamedDestinations(destinationsByName, pages);
    }

    private static bool TryReadExplicitDestination(IToken value, IPdfTokenScanner pdfScanner, Pages pages, ILog log, bool isRemoteDestination, out ExplicitDestination destination)
    {
        destination = null;

        if (DirectObjectFinder.TryGet(value, pdfScanner, out ArrayToken valueArray)
            && TryGetExplicitDestination(valueArray, pages, log, isRemoteDestination, out destination))
        {
            return true;
        }

        if (DirectObjectFinder.TryGet(value, pdfScanner, out DictionaryToken valueDictionary)
            && valueDictionary.TryGet(NameToken.D, pdfScanner, out valueArray)
            && TryGetExplicitDestination(valueArray, pages, log, isRemoteDestination, out destination))
        {
            return true;
        }

        return false;
    }
    
    internal static bool TryGetExplicitDestination(ArrayToken explicitDestinationArray, Pages pages, ILog log, bool isRemoteDestination, out ExplicitDestination destination)
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
            if (isRemoteDestination)
            {
                // Table 8.50 Remote Go-To Actions
                var errorMessage = $"{nameof(TryGetExplicitDestination)} Cannot use indirect reference for remote destination.";
                log?.Error(errorMessage);
                return false;
            }
            var page = pages.GetPageByReference(pageIndirectReferenceToken.Data);
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

            log?.Error(errorMessage);

            return false;
        }

        NameToken destTypeToken = null;
        if (explicitDestinationArray.Length > 1)
        {
            destTypeToken = explicitDestinationArray[1] as NameToken;
        }
        if (destTypeToken == null)
        {
            var errorMessage = $"Missing name token as second argument to explicit destination: {explicitDestinationArray}.";

            log?.Error(errorMessage);

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
}