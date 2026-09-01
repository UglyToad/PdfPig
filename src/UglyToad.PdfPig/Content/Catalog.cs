namespace UglyToad.PdfPig.Content
{
    using System;
    using Outline.Destinations;
    using Tokens;

    /// <summary>
    /// The root of the document's object hierarchy. Contains references to objects defining the contents,
    /// outline, named destinations and more.
    /// </summary>
    public sealed class Catalog
    {
        /// <summary>
        /// The catalog dictionary containing assorted information.
        /// </summary>
        public DictionaryToken CatalogDictionary { get; }

        internal NamedDestinations NamedDestinations { get; }

        internal Pages Pages { get; }

        /// <summary>
        /// The optional Version entry from the catalog dictionary, <see langword="null"/> where it is absent.
        /// It supersedes the file header version where it is later, see ISO 32000-2, 7.5.2.
        /// </summary>
        internal double? Version { get; }

        /// <summary>
        /// Create a new <see cref="CatalogDictionary"/>.
        /// </summary>
        internal Catalog(DictionaryToken catalogDictionary, Pages pages, NamedDestinations namedDestinations, double? version = null)
        {
            CatalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));
            Pages = pages ?? throw new ArgumentNullException(nameof(pages));
            NamedDestinations = namedDestinations;
            Version = version;
        }
    }
}
