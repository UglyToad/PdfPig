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
        /// <summary>Caps each decode buffer at 1 GiB, including predictor and LZW scratch space.</summary>
        public const int MaximumCapacity = 1024 * 1024 * 1024;

        // PDFBox Filter.decode() caps its estimated cache chunk size at 1 MiB.
        // https://github.com/apache/pdfbox/blob/3bac8e5ccae91216dd38d39f39fd7bfe9d4bca37/pdfbox/src/main/java/org/apache/pdfbox/filter/Filter.java
        internal const int MaximumInitialCapacity = 1 << 20;

        [Serializable]
        internal sealed class LimitExceededException : CorruptCompressedDataException
        {
            public LimitExceededException(string message = "The decompressed stream exceeds its output limit.") : base(message) { }
#if !NET
            private LimitExceededException(System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
        }

        public static void CheckLength(long length, int maximumCapacity = MaximumCapacity)
        {
            if (length > maximumCapacity)
            {
                throw new LimitExceededException();
            }
        }

        // PNG predictors include a filter byte per row; LZW also keeps literals and copy slack.
        // Bound that working data separately from the bytes returned to the caller.
        public static int WorkingCapacity(int outputLimit, int predictor, int overhead = 0)
            => (int)Math.Min(MaximumCapacity, (long)outputLimit * (predictor >= 10 ? 2 : 1) + 1 + overhead);

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
        public static int Capacity(int compressedLength, DictionaryToken streamDictionary, int factor, int minimum, int maximumExpansion, int maximumCapacity = MaximumCapacity)
        {
            maximumCapacity = Math.Min(maximumCapacity, MaximumInitialCapacity);
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
                return (int)Math.Min(maximumCapacity, stated + Slack);
            }

            return (int)Math.Min(maximumCapacity, Math.Max(minimum, (long)compressedLength * factor));
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
        /// that exceeds the permitted buffer size is refused, in every filter alike, rather
        /// than cut short in silence.
        /// </summary>
        public static void Grow(ref byte[] buffer, int written, long required, int maximumCapacity = MaximumCapacity)
        {
            if (required > maximumCapacity || buffer.Length >= maximumCapacity)
            {
                throw new LimitExceededException();
            }

            var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min(maximumCapacity, Math.Max(required, buffer.Length * 2L)));

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
