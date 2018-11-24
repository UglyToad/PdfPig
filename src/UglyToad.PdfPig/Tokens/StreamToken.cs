namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;
    using Filters;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A stream consists of a dictionary followed by zero or more bytes bracketed between the keywords stream and endstream.
    /// The bytes may be compressed by application of zero or more filters which are run in the order specified in the <see cref="StreamDictionary"/>.
    /// </summary>
    public class StreamToken : IDataToken<IReadOnlyList<byte>>
    {
        private readonly object lockObject = new object();

        private IReadOnlyList<byte> decodedBytes;

        /// <summary>
        /// The dictionary specifying the length of the stream, any applied compression filters and additional information.
        /// </summary>
        [NotNull]
        public DictionaryToken StreamDictionary { get; }

        /// <summary>
        /// The compressed byte data of the stream.
        /// </summary>
        [NotNull]
        public IReadOnlyList<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken([NotNull] DictionaryToken streamDictionary, [NotNull] IReadOnlyList<byte> data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        internal IReadOnlyList<byte> Decode(IFilterProvider filterProvider)
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

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Length: {Data.Count}, Dictionary: {StreamDictionary}";
        }
    }
}
