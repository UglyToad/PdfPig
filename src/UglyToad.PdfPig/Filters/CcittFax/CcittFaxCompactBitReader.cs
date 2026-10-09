using System;
using System.IO;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Filters.CcittFax;
// Each row policy fixes the encoding family and BlackIs1 polarity for a generic decoder call.
// Constant getters allow the JIT to remove branches that do not apply to that policy.
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

// Original C# implementation under the repository Apache-2.0 license.
// The 64-bit reservoir stores unread input bits. PeekBits inspects a prefix; ConsumeBits consumes bits.
// When the prefix is already buffered, PeekBits computes its value before loading another four
// bytes. This ordering allows prefix extraction and the next load to execute independently.
// Appending 32 bits is safe only when at most 32 unread bits remain. Short input tails are read
// byte by byte; missing bits may help index a table but cannot be consumed as input.
// The instruction-scheduling idea comes from these articles; implementation code was not copied:
// Dougall Johnson, "Reading bits with zero refill latency":
// https://dougallj.wordpress.com/2022/08/26/reading-bits-with-zero-refill-latency/
// Fabian Giesen, "Reading bits in far too many ways, part 2":
// https://fgiesen.wordpress.com/2018/02/20/reading-bits-in-far-too-many-ways-part-2/
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

    // PeekBits pads missing low bits with zero when input ends before a full lookup prefix.
    // ConsumeBits checks the actual code length against real buffered bits, so padding cannot
    // turn an incomplete code into valid input. A shorter complete code can still be decoded.
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

    // Compatibility decoding must distinguish exhausted input from an invalid code.
    // EnsureBits and the exact reads below never supply zero bits after the input ends.
    internal bool EnsureBits(int bitCount)
    {
        if (bufferedBitCount < bitCount)
            Refill(bitCount);
        return bufferedBitCount >= bitCount;
    }

    // Call only after EnsureBits(bitCount) returned true; the prefix is then fully buffered.
    internal int PeekBufferedBits(int bitCount) => (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1));
    internal int ReadBitsExact(int bitCount)
    {
        if (!EnsureBits(bitCount))
            throw new EndOfStreamException("Unexpected end of Huffman RLE stream");
        int prefixValue = PeekBufferedBits(bitCount);
        bufferedBitCount -= bitCount;
        return prefixValue;
    }
}
