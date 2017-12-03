namespace UglyToad.Pdf.Content
{
    using System;
    using ContentStream;
    using Cos;

    public class Catalog
    {
        private readonly PdfDictionary catalogDictionary;

        internal Catalog(PdfDictionary catalogDictionary)
        {
            this.catalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));
        }

        public CosBase Get(CosName name)
        {
            return catalogDictionary.GetItemOrDefault(name);
        }
    }
}
