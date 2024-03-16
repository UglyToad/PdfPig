﻿namespace UglyToad.PdfPig.Content
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
        /// Create a new <see cref="CatalogDictionary"/>.
        /// </summary>
        internal Catalog(DictionaryToken catalogDictionary, Pages pages, NamedDestinations namedDestinations)
        {
            CatalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));
            Pages = pages ?? throw new ArgumentNullException(nameof(pages));
            NamedDestinations = namedDestinations;
        }
    }
}
