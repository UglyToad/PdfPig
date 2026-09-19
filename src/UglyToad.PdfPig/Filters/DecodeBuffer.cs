namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Buffers;
    using UglyToad.PdfPig.Fonts;
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
        /// For a filter whose compression has no usable bound on how far it can expand, as Brotli
        /// has not: only <see cref="MaximumStatedLength"/> then limits a stated length.
        /// </summary>
        public const int UnboundedExpansion = int.MaxValue;

        /// <summary>
        /// A stated length above this is ignored, however plausible against the compressed length.
        /// The dictionary is data from the file and DL a hint by the specification (ISO 32000-1,
        /// 7.3.8.2); neither may decide how much is allocated before a byte has been decoded.
        /// Embedded files and font programs, the streams that state a length, are far smaller.
        /// </summary>
        internal const long MaximumStatedLength = 32L * 1024 * 1024;

        /// <summary>
        /// Room past a stated length, so that a decoder that has filled the buffer to the last byte
        /// does not have to grow it merely to find out that it is done.
        /// </summary>
        private const int Slack = 512;

        /// <summary>
        /// How large a buffer to decode into: the decoded length the dictionary states, as embedded
        /// files do with DL and font programs with Length1 and Length2, when it is plausible and
        /// modest, or else <paramref name="factor"/> times the compressed length, at least
        /// <paramref name="minimum"/>. Plausible means no more than <paramref name="maximumExpansion"/>
        /// times the compressed length, the most the filter's compression can expand by. A stated
        /// length is a hint; a buffer that was short grows.
        /// </summary>
        public static int Capacity(int compressedLength, DictionaryToken streamDictionary, int factor, int minimum, int maximumExpansion)
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

            if (IsPlausible(stated, compressedLength, maximumExpansion))
            {
                return (int)Math.Min(MaximumCapacity, stated + Slack);
            }

            return (int)Math.Min(MaximumCapacity, Math.Max(minimum, (long)compressedLength * factor));
        }

        /// <summary>
        /// Whether a decoded length the file states may size an allocation before anything has been
        /// decoded: positive, within <see cref="MaximumStatedLength"/>, and no more than the
        /// compression could expand <paramref name="compressedLength"/> bytes to.
        /// </summary>
        public static bool IsPlausible(long stated, int compressedLength, int maximumExpansion)
        {
            return stated > 0 && stated <= MaximumStatedLength && stated <= (long)compressedLength * maximumExpansion;
        }

        /// <summary>
        /// Replaces a rented buffer with a larger one, at least <paramref name="required"/> bytes and
        /// otherwise twice the size, carrying over the <paramref name="written"/> bytes. A stream
        /// that decodes to more than one array can hold is refused, in every filter alike, rather
        /// than cut short in silence.
        /// </summary>
        public static void Grow(ref byte[] buffer, int written, long required)
        {
            if (required > MaximumCapacity || buffer.Length >= MaximumCapacity)
            {
                throw new CorruptCompressedDataException("The stream decodes to more than can be held in one array.");
            }

            var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaximumCapacity, Math.Max(required, buffer.Length * 2L)));

            // Only what was written: the rest of a rented buffer is whatever the pool left in it.
            buffer.AsSpan(0, written).CopyTo(grown);
            ArrayPool<byte>.Shared.Return(buffer);

            buffer = grown;
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
