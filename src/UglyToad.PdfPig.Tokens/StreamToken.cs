namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A stream consists of a dictionary followed by zero or more bytes bracketed between the keywords stream and endstream.
    /// The bytes may be compressed by application of zero or more filters which are run in the order specified in the <see cref="StreamDictionary"/>.
    /// </summary>
    public class StreamToken : IDataToken<IReadOnlyList<byte>>
    {
        /// <summary>
        /// The dictionary specifying the length of the stream, any applied compression filters and additional information.
        /// </summary>
        public DictionaryToken StreamDictionary { get; }

        /// <summary>
        /// The compressed byte data of the stream.
        /// </summary>
        public IReadOnlyList<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="StreamToken"/>.
        /// </summary>
        /// <param name="streamDictionary">The stream dictionary.</param>
        /// <param name="data">The stream data.</param>
        public StreamToken(DictionaryToken streamDictionary, IReadOnlyList<byte> data)
        {
            StreamDictionary = streamDictionary ?? throw new ArgumentNullException(nameof(streamDictionary));
            Data = data ?? throw new ArgumentNullException(nameof(data));
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

            if (Data.Count != other.Data.Count)
            {
                return false;
            }

            for (var index = 0; index < Data.Count; ++index)
            {
                if (Data[index] != other.Data[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Length: {Data.Count}, Dictionary: {StreamDictionary}";
        }
    }
}
