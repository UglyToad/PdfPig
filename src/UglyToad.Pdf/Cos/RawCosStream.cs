namespace UglyToad.Pdf.Cos
{
    using System;
    using ContentStream;
    using Filters;

    internal class RawCosStream : CosBase
    {
        private static readonly object Lock = new object();

        public readonly byte[] streamBytes;

        private byte[] decodedBytes;

        public PdfDictionary Dictionary { get; }
        
        /// <summary>
        /// Combines the dictionary for the stream with the raw, encoded/filtered bytes.
        /// </summary>
        public RawCosStream(byte[] streamBytes, PdfDictionary streamDictionary)
        {
            this.streamBytes = streamBytes;

            Dictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
        }

        public byte[] Decode(IFilterProvider filterProvider)
        {
            lock (Lock)
            {
                if (decodedBytes != null)
                {
                    return decodedBytes;
                }

                var filters = filterProvider.GetFilters(Dictionary);

                var transform = streamBytes;
                for (var i = 0; i < filters.Count; i++)
                {
                    transform = filters[i].Decode(transform, Dictionary, i);
                }

                decodedBytes = transform;

                return transform;
            }
        }

        public override object Accept(ICosVisitor visitor)
        {
            throw new NotImplementedException("This used to implement using CosDictionary but I removed it! :O");
        }
    }
}
