using System;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Filters.CcittFax;
/// <summary>Supplies constant coding-family and output-polarity choices to generic decoder methods.</summary>
/// <remarks>
/// Value-type policies let the JIT specialize Group 4 framing and BlackIs1 painting.
/// .NET 8 and later use static interface properties; older targets use constrained instance
/// properties on a default struct without boxing.
/// </remarks>
internal interface ICcittRowPolicy
{
#if NET8_0_OR_GREATER
    static abstract bool IsGroup4 { get; }

    static abstract bool BlackIsOne { get; }

#else
    bool IsGroup4 { get; }

    bool BlackIsOne { get; }

#endif
}

/// <summary>Reads most-significant-bit-first CCITT codes from a span using a 64-bit reservoir.</summary>
/// <remarks>
/// <para>Algorithm: store unread bits in the low buffered-bit-count positions, with the next bit
/// at their upper end. Append four bytes in big-endian order when at most 32 bits remain;
/// refill short input tails byte by byte. Peek extracts a prefix without consuming its code.</para>
/// <para>When a complete prefix is already buffered, extract it before loading the next word.
/// This lets prefix decoding and refill proceed independently. Compact lookups may index using
/// zero-padded missing bits, but consumption checks the actual buffered count. Exact reads instead
/// report exhausted input, allowing signed decoding to distinguish truncation from invalid codes.</para>
/// <para>Idea provenance; no article or third-party implementation code was copied:</para>
/// <list type="table">
/// <listheader><term>Source</term><description>Adapted idea</description></listheader>
/// <item><term>Fabian Giesen</term><description>Stateful reservoirs with separate refill, peek
/// and consume operations, and the effect of their dependency chains.
/// See <see href="https://fgiesen.wordpress.com/2018/02/20/reading-bits-in-far-too-many-ways-part-2/">Reading bits in far too many ways, part 2</see>.</description></item>
/// <item><term>Dougall Johnson</term><description>Decode from an already buffered prefix while
/// preparing refill, removing refill from that prefix's dependency chain.
/// See <see href="https://dougallj.wordpress.com/2022/08/26/reading-bits-with-zero-refill-latency/">Reading bits with zero refill latency</see>.</description></item>
/// <item><term>PdfPig (Apache-2.0)</term><description>This bounded MSB-first span reader,
/// 32-bit refill, zero-padded lookup handling and exact-read failure semantics.</description></item>
/// </list>
/// </remarks>
internal ref struct CcittFaxCompactBitReader
{
    // The low bufferedBitCount bits are unread; bit bufferedBitCount - 1 is consumed next.
    // Higher bits are ignored.
    // inputOffset is the index of the next byte to append to bitBuffer.
    private readonly ReadOnlySpan<byte> compressedInput;
    private int inputOffset;
    private int bufferedBitCount;
    private ulong bitBuffer;
    internal CcittFaxCompactBitReader(ReadOnlySpan<byte> compressedInput)
    {
        this.compressedInput = compressedInput;
        inputOffset = 0;
        bufferedBitCount = 0;
        bitBuffer = 0;
    }

    // T keeps the generic call specialized with its decoder caller; bit extraction itself
    // is identical for every row policy.
    /// <summary>Peeks a code prefix, allowing zero padding only for lookup indexing.</summary>
    /// <remarks>The actual buffered count is unchanged by padding. ConsumeBits must check
    /// the selected code's true length before it is accepted.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int PeekBits<T>(int bitCount)
        where T : struct, ICcittRowPolicy
    {
        // Compute the prefix before loading and appending the next word. The guards ensure
        // that the prefix is complete and the 32-bit append cannot overflow the reservoir.
        if (bufferedBitCount >= bitCount && bufferedBitCount <= 32 && compressedInput.Length - inputOffset >= 4)
        {
            int prefixValue = (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1));
            ulong refilledBuffer = (bitBuffer << 32) | BinaryPrimitives.ReadUInt32BigEndian(compressedInput.Slice(inputOffset, 4));
            inputOffset += 4;
            bitBuffer = refilledBuffer;
            bufferedBitCount += 32;
            return prefixValue;
        }

        if (bufferedBitCount < bitCount)
            Refill(bitCount);
        return bufferedBitCount >= bitCount
            ? (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1))
            : (int)((bitBuffer << (bitCount - bufferedBitCount)) & ((1UL << bitCount) - 1));
    }

    private void Refill(int bitCount)
    {
        if (compressedInput.Length - inputOffset >= 4 && bufferedBitCount <= 32)
        {
            bitBuffer = (bitBuffer << 32) | BinaryPrimitives.ReadUInt32BigEndian(compressedInput.Slice(inputOffset, 4));
            bufferedBitCount += 32;
            inputOffset += 4;
        }

        while (bufferedBitCount < bitCount && inputOffset < compressedInput.Length)
        {
            bitBuffer = (bitBuffer << 8) | compressedInput[inputOffset++];
            bufferedBitCount += 8;
        }
    }

    /// <summary>Consumes real buffered bits and rejects an incomplete code.</summary>
    /// <exception cref="InvalidDataException">The requested code length exceeds real buffered bits.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ConsumeBits(int bitCount)
    {
        if (bufferedBitCount < bitCount)
            throw new InvalidDataException("Truncated CCITT code.");
        bufferedBitCount -= bitCount;
    }

    internal int ReadBits<T>(int bitCount)
        where T : struct, ICcittRowPolicy
    {
        var prefixValue = PeekBits<T>(bitCount);
        ConsumeBits(bitCount);
        return prefixValue;
    }

    internal void AlignToByteBoundary() => bufferedBitCount -= bufferedBitCount & 7;
    // Group 3 rows start with an end-of-line (EOL) code: at least eleven zeros followed by one.
    // Mixed Group 3 adds a tag bit: one selects 1D, zero selects 2D decoding.
    // Modified Huffman and Group 4 do not use per-row EOL synchronization.
    internal bool ReadRowIsOneDimensional<T>(CcittFaxCompressionType compressionType, bool encodedByteAlign)
        where T : struct, ICcittRowPolicy
    {
        if (encodedByteAlign)
            AlignToByteBoundary();
        if (compressionType == CcittFaxCompressionType.ModifiedHuffman)
            return true;
        if (compressionType == CcittFaxCompressionType.Group4_2D)
            return false;
        var precedingZeroCount = 0;
        while (true)
        {
            if (ReadBits<T>(1) == 0)
                precedingZeroCount++;
            else if (precedingZeroCount >= 11)
                break;
            else
                precedingZeroCount = 0;
        }

        return compressionType == CcittFaxCompressionType.Group3_1D || ReadBits<T>(1) != 0;
    }

    internal int ReadModeBitByBit<T>()
        where T : struct, ICcittRowPolicy
    {
        // Vertical codes give offsets from a reference-row transition: 1 means zero,
        // 011/010 mean +1/-1. Horizontal (001) reads two runs; pass (0001) skips a reference pair.
        if (ReadBits<T>(1) != 0)
            return 0;
        var middleBits = ReadBits<T>(2);
        if (middleBits == 1)
            return 100;
        if (middleBits >= 2)
            return middleBits == 3 ? 1 : -1;
        if (ReadBits<T>(1) != 0)
            return 101;
        var tailBits = ReadBits<T>(2);
        if (tailBits >= 2)
            return tailBits == 3 ? 2 : -2;
        if (tailBits == 1)
            return ReadBits<T>(1) != 0 ? 3 : -3;
        throw new InvalidDataException("Unsupported extension, EOFB or invalid CCITT mode.");
    }

    /// <summary>Refills until the requested prefix is present or compressed input ends.</summary>
    /// <returns>True only when all requested bits come from real input.</returns>
    internal bool EnsureBits(int bitCount)
    {
        if (bufferedBitCount < bitCount)
            Refill(bitCount);
        return bufferedBitCount >= bitCount;
    }

    // Call only after EnsureBits(bitCount) returned true; the prefix is then fully buffered.
    internal int PeekBufferedBits(int bitCount) => (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1));
    /// <summary>Reads and consumes a complete code fragment without zero padding.</summary>
    /// <exception cref="EndOfStreamException">Compressed input ends before the requested bits are available.</exception>
    internal int ReadBitsExact(int bitCount)
    {
        if (!EnsureBits(bitCount))
            throw new EndOfStreamException("Unexpected end of Huffman RLE stream");
        int prefixValue = PeekBufferedBits(bitCount);
        bufferedBitCount -= bitCount;
        return prefixValue;
    }
}
