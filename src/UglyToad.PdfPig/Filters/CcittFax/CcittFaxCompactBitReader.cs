using System;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Filters.CcittFax;
// Feature policies keep constants visible to the JIT instead of branching on runtime configuration.
internal interface ICcittCompactFeatures
{
#if NET8_0_OR_GREATER
    static abstract bool Group4 { get; }
    static abstract bool Table16 { get; }
    static abstract bool Preload { get; }
    static abstract bool Overlap { get; }
    static abstract bool BlackOne { get; }
    static abstract bool WholeBitmap { get; }
#else
    bool Group4 { get; }

    bool Table16 { get; }

    bool Preload { get; }

    bool Overlap { get; }

    bool BlackOne { get; }

    bool WholeBitmap { get; }
#endif
}

// Original C# implementation under the repository Apache-2.0 license.
// The MSB-first reservoir separates prefix extraction from refilling: when a complete prefix is
// already buffered, Peek can extract it from the old reservoir while loading the next 32-bit word.
// The append is allowed only with four real input bytes and at most 32 buffered bits, so it fits
// in 64 bits. Ordinary refill handles short tails; consuming a padded lookup prefix still fails.
// Scheduling/dataflow ideas were informed by these articles, not by copied implementation code:
// Dougall Johnson, "Reading bits with zero refill latency":
// https://dougallj.wordpress.com/2022/08/26/reading-bits-with-zero-refill-latency/
// Fabian Giesen, "Reading bits in far too many ways, part 2":
// https://fgiesen.wordpress.com/2018/02/20/reading-bits-in-far-too-many-ways-part-2/
internal ref struct CcittFaxCompactBitReader
{
    // Valid unread bits occupy the low "count" bits, with the next bit at count - 1.
    // offset points just beyond the bytes already appended. Consumed high bits need not be cleared.
    private readonly ReadOnlySpan<byte> input;
    private int offset, count;
    private ulong reservoir;
    internal CcittFaxCompactBitReader(ReadOnlySpan<byte> input)
    {
        this.input = input;
        offset = 0;
        count = 0;
        reservoir = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Peek<T>(int length)
        where T : struct, ICcittCompactFeatures
    {
        // Decode from the old reservoir while an independent next-word load is prepared.
        // Only append a real word when the old prefix is complete and the append fits in 64 bits.
#if NET8_0_OR_GREATER
        if (T.Overlap && count >= length && count <= 32 && input.Length - offset >= 4)
#else
        if (default(T).Overlap && count >= length && count <= 32 && input.Length - offset >= 4)
#endif
        {
            int value = (int)((reservoir >> (count - length)) & ((1UL << length) - 1));
            ulong pending = (reservoir << 32) | BinaryPrimitives.ReadUInt32BigEndian(input.Slice(offset, 4));
            offset += 4;
            reservoir = pending;
            count += 32;
            return value;
        }

        if (count < length)
            Refill(length);
        return count >= length ? (int)((reservoir >> (count - length)) & ((1UL << length) - 1)) : (int)((reservoir << (length - count)) & ((1UL << length) - 1));
    }

    private void Refill(int length)
    {
        if (input.Length - offset >= 4 && count <= 32)
        {
            reservoir = (reservoir << 32) | BinaryPrimitives.ReadUInt32BigEndian(input.Slice(offset, 4));
            count += 32;
            offset += 4;
        }

        while (count < length && offset < input.Length)
        {
            reservoir = (reservoir << 8) | input[offset++];
            count += 8;
        }
    }

    // Peek may zero-pad a short prefix for lookup, but Drop can consume only real input bits.
    // This separation prevents a lookup from silently accepting a truncated code.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Drop(int length)
    {
        if (count < length)
            throw new InvalidDataException("Truncated CCITT code.");
        count -= length;
    }

    internal int Read<T>(int length)
        where T : struct, ICcittCompactFeatures
    {
        var value = Peek<T>(length);
        Drop(length);
        return value;
    }

    internal void Align() => count -= count & 7;
    internal bool OneDimensional<T>(CcittFaxCompressionType mode, bool aligned)
        where T : struct, ICcittCompactFeatures
    {
        if (aligned)
            Align();
        if (mode == CcittFaxCompressionType.ModifiedHuffman)
            return true;
        if (mode == CcittFaxCompressionType.Group4_2D)
            return false;
        var zeros = 0;
        while (true)
        {
            if (Read<T>(1) == 0)
                zeros++;
            else if (zeros >= 11)
                break;
            else
                zeros = 0;
        }

        return mode == CcittFaxCompressionType.Group3_1D || Read<T>(1) != 0;
    }

    internal int Mode<T>()
        where T : struct, ICcittCompactFeatures
    {
        // 1 = V0, 011/010 = V+1/V-1, 001 = horizontal, 0001 = pass.
        if (Read<T>(1) != 0)
            return 0;
        var pair = Read<T>(2);
        if (pair == 1)
            return 100;
        if (pair >= 2)
            return pair == 3 ? 1 : -1;
        if (Read<T>(1) != 0)
            return 101;
        var tail = Read<T>(2);
        if (tail >= 2)
            return tail == 3 ? 2 : -2;
        if (tail == 1)
            return Read<T>(1) != 0 ? 3 : -3;
        throw new InvalidDataException("Unsupported extension, EOFB or invalid CCITT mode.");
    }

    // Exact reads preserve EOF versus invalid-prefix semantics on full-width rows.
    internal bool Ensure(int length)
    {
        if (count < length)
            Refill(length);
        return count >= length;
    }

    // Caller must first establish Ensure(length); unlike Peek, this never pads a prefix.
    internal int PeekExact(int length) => (int)((reservoir >> (count - length)) & ((1UL << length) - 1));
    internal int ReadExact(int length)
    {
        if (!Ensure(length))
            throw new EndOfStreamException("Unexpected end of Huffman RLE stream");
        int value = PeekExact(length);
        count -= length;
        return value;
    }
}
