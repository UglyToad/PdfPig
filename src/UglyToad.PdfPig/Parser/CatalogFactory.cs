namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Globalization;
    using Content;
    using Core;
    using Logging;
    using Outline.Destinations;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class CatalogFactory
    {
        public static Catalog Create(IndirectReference rootReference, DictionaryToken dictionary,
            IPdfTokenScanner scanner, PageFactory pageFactory, ILog log, bool isLenientParsing)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (dictionary.TryGet(NameToken.Type, out var type) && !ReferenceEquals(type, NameToken.Catalog)
                && !isLenientParsing)
            {
                throw new PdfDocumentFormatException($"The type of the catalog dictionary was not Catalog: {dictionary}.");
            }

            if (!dictionary.TryGet(NameToken.Pages, out var value))
            {
                throw new PdfDocumentFormatException($"No pages entry was found in the catalog dictionary: {dictionary}.");
            }

            DictionaryToken? pagesDictionary;
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

            if (pagesDictionary is null)
            {
                if (isLenientParsing)
                {
                    pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());
                }
                else
                {
                    throw new PdfDocumentFormatException("Pages entry is null.");
                }
            }

            var pages = PagesFactory.Create(pagesReference, pagesDictionary, scanner, pageFactory, log, isLenientParsing);
            var namedDestinations = NamedDestinationsProvider.Read(dictionary, scanner, pages, null);

            var version = GetVersion(dictionary, scanner, log);

            return new Catalog(dictionary, pages, namedDestinations, version);
        }

        /// <summary>
        /// Get the optional Version entry from the catalog dictionary. A document upgraded by an incremental
        /// update cannot have its header rewritten, so the later version is declared in the catalog instead.
        /// </summary>
        /// <returns>
        /// <see langword="null"/> where the entry is absent or unreadable, in which case the header version stands.
        /// </returns>
        internal static double? GetVersion(DictionaryToken dictionary, IPdfTokenScanner scanner, ILog log)
        {
            if (!dictionary.TryGet(NameToken.Version, out var token))
            {
                return null;
            }

            // The value is a name object, for example /2.0.
            if (DirectObjectFinder.TryGet(token, scanner, out NameToken? name)
                && double.TryParse(name.Data, NumberStyles.Number, CultureInfo.InvariantCulture, out var version))
            {
                return version;
            }

            // A number is not valid here but is written by some producers.
            if (DirectObjectFinder.TryGet(token, scanner, out NumericToken? numeric))
            {
                return numeric.Data;
            }

            log.Warn($"The version entry in the catalog dictionary was not a version number, using the header version instead: {token}.");

            return null;
        }
    }
}
