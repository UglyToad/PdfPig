namespace UglyToad.PdfPig.Content
{
    using System;
    using Tokens;

    internal class Catalog
    {
        private readonly DictionaryToken catalogDictionary;

        public DictionaryToken PagesDictionary { get; }

        public Catalog(DictionaryToken catalogDictionary, DictionaryToken pagesDictionary)
        {
            this.catalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));

            PagesDictionary = pagesDictionary ?? throw new ArgumentNullException(nameof(pagesDictionary));
        }
    }
}
