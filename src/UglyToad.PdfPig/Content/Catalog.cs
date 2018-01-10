namespace UglyToad.PdfPig.Content
{
    using System;
    using ContentStream;
    using Cos;

    internal class Catalog
    {
        private readonly PdfDictionary catalogDictionary;

        public PdfDictionary PagesDictionary { get; }

        internal Catalog(PdfDictionary catalogDictionary, PdfDictionary pagesDictionary)
        {
            this.catalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));

            PagesDictionary = pagesDictionary ?? throw new ArgumentNullException(nameof(pagesDictionary));
        }

        public CosBase Get(CosName name)
        {
            return catalogDictionary.GetItemOrDefault(name);
        }
    }
}
