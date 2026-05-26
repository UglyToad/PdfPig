namespace UglyToad.PdfPig.Tokens
{
    using System;

    /// <summary>
    /// A stream consists of a dictionary followed by zero or more bytes bracketed between the keywords stream and endstream.
    /// The bytes may be compressed by application of zero or more filters which are run in the order specified in the <see cref="StreamDictionary"/>.
    /// </summary>
    public sealed class StreamToken : IDataToken<Memory<byte>>
    {
        private readonly Lazy<Memory<byte>> lazyData;

        /// <summary>
        /// The dictionary specifying the length of the stream, any applied compression filters and additional information.
        /// </summary>
        public DictionaryToken StreamDictionary { get; }

        /// <summary>
        /// The compressed byte data of the stream.
        /// </summary>
        public Memory<byte> Data => lazyData.Value;

        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken(DictionaryToken streamDictionary, byte[] data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            lazyData = new Lazy<Memory<byte>>(() => (Memory<byte>)data);
        }

        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken(DictionaryToken streamDictionary, Memory<byte> data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            lazyData = new Lazy<Memory<byte>>(() => data);
        }

        /// <summary>
        /// Create a new <see cref="StreamToken"/> with deferred data loading.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="dataFactory">A factory function that produces the stream data on first access.</param>
        internal StreamToken(DictionaryToken streamDictionary, Func<Memory<byte>> dataFactory)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            if (dataFactory is null)
            {
                throw new ArgumentNullException(nameof(dataFactory));
            }

            lazyData = new Lazy<Memory<byte>>(dataFactory);
        }

        /// <summary>
        /// Whether the stream byte data has been loaded into memory.
        /// Returns <see langword="false"/> when the stream was created with deferred loading
        /// and the data has not yet been accessed.
        /// </summary>
        internal bool IsDataLoaded => lazyData.IsValueCreated;

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is StreamToken other))
            {
                return false;
            }

            if (!StreamDictionary.Equals(other.StreamDictionary))
            {
                return false;
            }

            return Data.Span.SequenceEqual(other.Data.Span);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (!lazyData.IsValueCreated)
            {
                return $"Length: deferred, Dictionary: {StreamDictionary}";
            }

            return $"Length: {Data.Length}, Dictionary: {StreamDictionary}";
        }
    }
}
