// Frozen pre-consolidation test oracle. Never used by production decoding.
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Tests.Filters.CcittBaseline;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.CcittFax;
/// <summary>MSB-first memory cursor with bounded block refill.</summary>
internal ref struct CcittFaxBitReader
{
    private readonly ReadOnlySpan<byte> input;
    internal int Offset, Count;
    internal ulong Reservoir;
    internal CcittFaxBitReader(ReadOnlySpan<byte> input, int offset, int count, ulong reservoir)
    {
        this.input = input;
        Offset = offset;
        Count = count;
        Reservoir = reservoir;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Ensure(int length)
    {
        if (Count >= length)
            return true;
        if (input.Length - Offset >= 4 && Count <= 32)
        {
            Reservoir = (Reservoir << 32) | BinaryPrimitives.ReadUInt32BigEndian(input.Slice(Offset, 4));
            Offset += 4;
            Count += 32;
        }
        while (Count < length && Offset < input.Length)
        {
            Reservoir = (Reservoir << 8) | input[Offset++];
            Count += 8;
        }
        return Count >= length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Peek(int length) => (int)((Reservoir >> (Count - length)) & ((1UL << length) - 1));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Read(int length)
    {
        if (!Ensure(length))
            throw new EndOfStreamException("Unexpected end of Huffman RLE stream");
        int value = Peek(length);
        Count -= length;
        return value;
    }

    internal void Align() => Count -= Count & 7;
}
