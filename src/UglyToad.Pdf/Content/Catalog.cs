namespace UglyToad.Pdf.Content
{
    using System;
    using ContentStream;
    using Cos;

    public class Catalog
    {
        private readonly ContentStreamDictionary catalogDictionary;

        internal Catalog(ContentStreamDictionary catalogDictionary)
        {
            this.catalogDictionary = catalogDictionary ?? throw new ArgumentNullException(nameof(catalogDictionary));
        }

        public CosBase Get(CosName name)
        {
            return catalogDictionary.GetItemOrDefault(name);
        }
    }
}
