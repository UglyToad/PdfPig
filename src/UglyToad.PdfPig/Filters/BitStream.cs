namespace UglyToad.PdfPig.Filters
{
    using System;

    internal ref struct BitStream
    {
        private readonly ReadOnlySpan<byte> data;

        private int currentWithinByteBitOffset;
        private int currentByteIndex;

        public BitStream(ReadOnlySpan<byte> data)
        {
            this.data = data;
        }

        public int Get(int numberOfBits)
        {
            var endWithinByteBitOffset = (numberOfBits + currentWithinByteBitOffset) % 8;

            var numberOfBytesToRead = (numberOfBits + currentWithinByteBitOffset) / 8;

            if (endWithinByteBitOffset != 0)
            {
                numberOfBytesToRead++;
            }

            var result = 0;
            for (var i = 0; i < numberOfBytesToRead; i++)
            {
                if (i > 0)
                {
                    currentByteIndex++;
                }

                if (currentByteIndex >= data.Length)
                {
                    throw new InvalidOperationException($"Reached the end of the bit stream while trying to read {i} bits.");
                }

                result <<= 8;
                result |= data[currentByteIndex];
            }

            // Trim trailing bits.
            if (endWithinByteBitOffset > 0)
            {
                result >>= 8 - endWithinByteBitOffset;
            }
            else
            {
                currentByteIndex++;
            }

            // 'And' out the leading bits.
            var firstBitOfDataWithinInt = (sizeof(int) * 8) - numberOfBits;
            result &= (int)(0xffffffff >> firstBitOfDataWithinInt);
            
            currentWithinByteBitOffset = endWithinByteBitOffset;

            return result;
        }
    }
}
