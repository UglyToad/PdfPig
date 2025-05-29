namespace UglyToad.PdfPig.Tokens
{
    using System;

    /// <summary>
    /// A stream consists of a dictionary followed by zero or more bytes bracketed between the keywords stream and endstream.
    /// The bytes may be compressed by application of zero or more filters which are run in the order specified in the <see cref="StreamDictionary"/>.
    /// </summary>
    public sealed class StreamToken : IDataToken<Memory<byte>>
    {
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
            return $"Length: {Data.Length}, Dictionary: {StreamDictionary}";
        }
    }
}
