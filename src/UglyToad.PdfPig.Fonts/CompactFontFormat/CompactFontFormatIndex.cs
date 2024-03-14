﻿namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class CompactFontFormatIndex : IReadOnlyList<IReadOnlyList<byte>>
    {
        private readonly IReadOnlyList<IReadOnlyList<byte>> bytes;

        public int Count => bytes.Count;

        public IReadOnlyList<byte> this[int index] => bytes[index];

        public static CompactFontFormatIndex None { get; } = new CompactFontFormatIndex(new byte[0][]);

        public CompactFontFormatIndex(byte[][] bytes)
        {
            this.bytes = bytes ?? Array.Empty<IReadOnlyList<byte>>();
        }
        
        public IEnumerator<IReadOnlyList<byte>> GetEnumerator()
        {
            return bytes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
