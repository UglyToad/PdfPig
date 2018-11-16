namespace UglyToad.PdfPig.Tokens
{
    using Filters;

    internal class StreamToken : IDataToken<byte[]>
    {
        private readonly object lockObject = new object();

        private byte[] decodedBytes;

        public DictionaryToken StreamDictionary { get; }

        public byte[] Data { get; }

        public StreamToken(DictionaryToken streamDictionary, byte[] data)
        {
            StreamDictionary = streamDictionary;
            Data = data;
        }

        public byte[] Decode(IFilterProvider filterProvider)
        {
            lock (lockObject)
            {
                if (decodedBytes != null)
                {
                    return decodedBytes;
                }
                
                var filters = filterProvider.GetFilters(StreamDictionary);

                var transform = Data;
                for (var i = 0; i < filters.Count; i++)
                {
                    transform = filters[i].Decode(transform, StreamDictionary, i);
                }

                decodedBytes = transform;

                return transform;
            }
        }
    }
}
