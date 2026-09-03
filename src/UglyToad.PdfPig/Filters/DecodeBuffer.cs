namespace UglyToad.PdfPig.Filters
{
    using System;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// How large a buffer a filter decodes into: the length the stream dictionary states where it
    /// states one, so that such a stream neither grows the buffer nor rents far more than it needs.
    /// </summary>
    internal static class DecodeBuffer
    {
        /// <summary>The largest array the runtime will hand out.</summary>
        public const int MaximumCapacity = 0x7FFFFFC7;

        /// <summary>
        /// A stated length beyond this many times the compressed length is not to be trusted.
        /// Deflate expands by 1032 at the very most, Brotli by more; four thousand leaves room for
        /// both and shuts out the nonsense a damaged dictionary can state.
        /// </summary>
        private const int MaximumExpansion = 4096;

        /// <summary>
        /// Room past a stated length, so that a decoder that has filled the buffer to the last byte
        /// does not have to grow it merely to find out that it is done.
        /// </summary>
        private const int Slack = 512;

        /// <summary>
        /// How large a buffer to decode into: the decoded length the dictionary states, as embedded
        /// files do with DL and font programs with Length1 and Length2, or else
        /// <paramref name="factor"/> times the compressed length, at least <paramref name="minimum"/>.
        /// </summary>
        public static int Capacity(int compressedLength, DictionaryToken streamDictionary, int factor, int minimum)
        {
            long stated = 0;

            if (TryGetLength(streamDictionary, NameToken.Dl, out var decodedLength))
            {
                stated = decodedLength;
            }
            else
            {
                if (TryGetLength(streamDictionary, NameToken.Length1, out var length1))
                {
                    stated = length1;
                }

                if (TryGetLength(streamDictionary, NameToken.Length2, out var length2))
                {
                    stated += length2;
                }
            }

            if (stated > 0 && stated <= (long)compressedLength * MaximumExpansion + minimum)
            {
                return (int)Math.Min(MaximumCapacity, stated + Slack);
            }

            return (int)Math.Min(MaximumCapacity, Math.Max(minimum, (long)compressedLength * factor));
        }

        private static bool TryGetLength(DictionaryToken streamDictionary, NameToken key, out long length)
        {
            length = 0;

            if (streamDictionary.TryGet(key, out var token) && token is NumericToken number && number.Int > 0)
            {
                length = number.Int;
                return true;
            }

            return false;
        }
    }
}
