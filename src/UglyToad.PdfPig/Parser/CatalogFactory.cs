namespace UglyToad.PdfPig.Parser
{
    using System;
    using Content;
    using Core;
    using Logging;
    using Outline;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class CatalogFactory
    {
        public static Catalog Create(IndirectReference rootReference, DictionaryToken dictionary,
            IPdfTokenScanner scanner, PageFactory pageFactory, ILog log, bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (dictionary.TryGet(NameToken.Type, out var type) && !ReferenceEquals(type, NameToken.Catalog))
            {
                throw new PdfDocumentFormatException($"The type of the catalog dictionary was not Catalog: {dictionary}.");
            }

            if (!dictionary.TryGet(NameToken.Pages, out var value))
            {
                throw new PdfDocumentFormatException($"No pages entry was found in the catalog dictionary: {dictionary}.");
            }

            DictionaryToken pagesDictionary;
            var pagesReference = rootReference;

            if (value is IndirectReferenceToken pagesRef)
            {
                pagesReference = pagesRef.Data;
                pagesDictionary = DirectObjectFinder.Get<DictionaryToken>(pagesRef, scanner);
            }
            else if (value is DictionaryToken pagesDict)
            {
                pagesDictionary = pagesDict;
            }
            else
            {
                pagesDictionary = DirectObjectFinder.Get<DictionaryToken>(value, scanner);
            }

            var pages = PagesFactory.Create(pagesReference, pagesDictionary, scanner, pageFactory, log, isLenientParsing);
            var namedDestinations = NamedDestinationsProvider.Read(dictionary, scanner, pages, null);

            return new Catalog(dictionary, pages, namedDestinations);
        }
    }
}
