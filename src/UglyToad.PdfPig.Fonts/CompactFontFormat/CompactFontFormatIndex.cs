namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class CompactFontFormatIndex : IReadOnlyList<ReadOnlyMemory<byte>>
    {
        private readonly byte[][] bytes;

        public int Count => bytes.Length;

        public ReadOnlyMemory<byte> this[int index] => bytes[index];

        public static CompactFontFormatIndex None { get; } = new CompactFontFormatIndex([]);

        public CompactFontFormatIndex(byte[][] bytes)
        {
            this.bytes = bytes ?? [];
        }
        
        public IEnumerator<ReadOnlyMemory<byte>> GetEnumerator()
        {
            foreach (var item in bytes)
            {
                yield return new ReadOnlyMemory<byte>(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in bytes)
            {
                yield return new ReadOnlyMemory<byte>(item);
            }

        }
    }
}
