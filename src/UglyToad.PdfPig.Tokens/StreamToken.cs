namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Threading;

    /// <summary>
    /// A stream consists of a dictionary followed by zero or more bytes bracketed between the keywords stream and endstream.
    /// The bytes may be compressed by application of zero or more filters which are run in the order specified in the <see cref="StreamDictionary"/>.
    /// </summary>
    public sealed class StreamToken : IDataToken<Memory<byte>>
    {
        private int hashCode;
        private bool hashCodeComputed;

        /// <summary>
        /// The dictionary specifying the length of the stream, any applied compression filters and additional information.
        /// </summary>
        public DictionaryToken StreamDictionary { get; }

        /// <summary>
        /// The compressed byte data of the stream.
        /// </summary>
        public Memory<byte> Data { get; }
        
        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken(DictionaryToken streamDictionary, byte[] data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken(DictionaryToken streamDictionary, Memory<byte> data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            Data = data;
        }
        
        private int ComputeHashCode()
        {
            var hash = new HashCode();
            hash.Add(StreamDictionary);
#if NET6_0_OR_GREATER
            hash.AddBytes(Data.Span);
#else
            var span = Data.Span;
            for (var i = 0; i < span.Length; i++)
            {
                hash.Add(span[i]);
            }
#endif
            return hash.ToHashCode();
        }
        
        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!Volatile.Read(ref hashCodeComputed))
            {
                hashCode = ComputeHashCode();
                Volatile.Write(ref hashCodeComputed, true);
            }

            return hashCode;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IToken token && Equals(token);
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not StreamToken other)
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
            return $"Length: {Data.Length}, Dictionary: {StreamDictionary}";
        }
    }
}
