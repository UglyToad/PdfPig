namespace UglyToad.PdfPig.Content
{
    using System;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The root of the document's object hierarchy. Contains references to objects defining the contents,
    /// outline, named destinations and more.
    /// </summary>
    public class Catalog
    {
        /// <summary>
        /// The catalog dictionary containing assorted information.
        /// </summary>
        [NotNull]
        public DictionaryToken CatalogDictionary { get; }

        /// <summary>
        /// Defines the page tree node which is the root of the pages tree for the document.
        /// </summary>
        [NotNull]
        public DictionaryToken PagesDictionary { get; }

        /// <summary>
        /// Create a new <see cref="CatalogDictionary"/>.
        /// </summary>
        internal Catalog(DictionaryToken catalogDictionary, DictionaryToken pagesDictionary)
        {
            CatalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));

            PagesDictionary = pagesDictionary ?? throw new ArgumentNullException(nameof(pagesDictionary));
        }
    }
}
