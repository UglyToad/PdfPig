namespace UglyToad.PdfPig.Filters.Lzw
{
    using System;
    using System.Buffers.Binary;

    /// <summary>
    /// Reads big-endian bit fields of varying width from a byte sequence, the way LZW codes are packed.
    /// </summary>
    /// <remarks>
    /// A field is served from the eight bytes at its position, loaded as one word, so a code is a
    /// load, a shift and a mask with no state to refill. Only the last few bytes take a slower path.
    /// </remarks>
    internal ref struct BitStream
    {
        private const int MaxBits = 32;

        private const int WordBits = sizeof(ulong) * 8;

        private readonly ReadOnlySpan<byte> data;

        /// <summary>The next bit to read, counted from the start of the data.</summary>
        private long bitPosition;

        public BitStream(ReadOnlySpan<byte> data)
        {
            this.data = data;
        }

        /// <summary>
        /// Reads the next <paramref name="numberOfBits"/> bits as an unsigned value.
        /// </summary>
        /// <exception cref="InvalidOperationException">The data ended before the bits could be read.</exception>
        public int Get(int numberOfBits)
        {
            if (!TryGet(numberOfBits, out var result))
            {
                throw new InvalidOperationException($"Reached the end of the bit stream while trying to read {numberOfBits} bits.");
            }

            return result;
        }

        /// <summary>
        /// Reads the next <paramref name="numberOfBits"/> bits as an unsigned value, or reports that
        /// the data ended before a full value was available.
        /// </summary>
        public bool TryGet(int numberOfBits, out int result)
        {
            if (numberOfBits <= 0 || numberOfBits > MaxBits)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfBits), $"Can read between 1 and {MaxBits} bits, not {numberOfBits}.");
            }

            var byteIndex = (int)(bitPosition >> 3);
            var bitOffset = (int)(bitPosition & 7);
            var mask = (1UL << numberOfBits) - 1;

            if (byteIndex + sizeof(ulong) <= data.Length)
            {
                // The field starts bitOffset bits into this word and is at most 32 bits, so it
                // always fits: 7 + 32 < 64.
                var word = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(byteIndex));

                result = (int)((word >> (WordBits - bitOffset - numberOfBits)) & mask);
                bitPosition += numberOfBits;
                return true;
            }

            // Fewer than eight bytes left: gather what there is.
            var remainingBits = ((long)data.Length << 3) - bitPosition;

            if (remainingBits < numberOfBits)
            {
                result = 0;
                return false;
            }

            ulong tail = 0;
            for (var i = byteIndex; i < data.Length; i++)
            {
                tail = (tail << 8) | data[i];
            }

            var tailBits = (data.Length - byteIndex) * 8;

            result = (int)((tail >> (tailBits - bitOffset - numberOfBits)) & mask);
            bitPosition += numberOfBits;
            return true;
        }
    }
}
