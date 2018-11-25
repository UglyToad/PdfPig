using System;

namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    internal class CompactFontFormatIndexReader
    {
        public CompactFontFormatIndex ReadDictionaryData(CompactFontFormatData data)
        {
            var index = ReadIndex(data);

            if (index.Length == 0)
            {
                return new CompactFontFormatIndex(null);
            }

            var count = index.Length - 1;

            var results = new byte[count][];

            for (var i = 0; i < count; i++)
            {
                var length = index[i + 1] - index[i];

                if (length < 0)
                {
                    throw new InvalidOperationException($"Negative object length {length} at {i}. Current position: {data.Position}.");
                }

                if (length > data.Length)
                {
                    throw new InvalidOperationException($"Attempted to read data of length {length} in data array of length {data.Length}.");
                }

                results[i] = data.ReadBytes(length);
            }

            return new CompactFontFormatIndex(results);
        }

        public int[] ReadIndex(CompactFontFormatData data)
        {
            var count = data.ReadCard16();

            if (count == 0)
            {
                return Array.Empty<int>();
            }

            var offsetSize = data.ReadOffsize();

            var offsets = new int[count + 1];

            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = data.ReadOffset(offsetSize);
            }

            return offsets;
        }
    }
}
