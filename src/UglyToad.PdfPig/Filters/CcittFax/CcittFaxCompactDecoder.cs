using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Filters.CcittFax;
/// <summary>
/// Decodes CCITT rows into a bitmap using color-change positions as the 2D reference.
/// The compact path stores positions in UInt16. Wider rows and input that this path cannot
/// handle are retried using signed Int32 positions and the requested parsing mode.
/// </summary>
// Original C# implementation under the repository Apache-2.0 license.
// A transition is the pixel position where a row changes between white and black. The previous
// row supplies reference positions for 2D decoding. Standard T.4/T.6 codewords index flat tables
// instead of a binary tree. One packed entry decodes two short runs; longer runs are decoded
// individually and checked against the remaining row width.
// Rows start white, including unused bits at the end of each row. Black intervals are painted
// as ones or zeros according to BlackIs1, avoiding a separate inversion pass.
// Next-operation preloading was inspired by the MIT-licensed libdeflate decompression fast loop:
// https://github.com/ebiggers/libdeflate/blob/master/lib/decompress_template.h
// The next CCITT mode is peeked before painting and consumed on the next iteration, allowing
// independent bit decoding and pixel writes to overlap. This is an original adaptation of the idea;
// no libdeflate implementation code or lookup tables were copied.
internal static partial class CcittFaxCompactDecoder
{
    private const int HorizontalMode = 100;
    private const int PassMode = 101;
    private const int UnknownMode = int.MinValue;
    private const int EndOfLineRunMarker = -2000;

    // Bits 0..3 store the consumed code length; bits 4..15 store its pixel count.
    // A zero entry marks an unmapped prefix, not a zero-pixel run. A run uses zero or more
    // makeup codes (multiples of 64), then a terminating code (0..63).
    private static readonly ushort[] WhiteRunLookup = BuildRunLookup(CcittFaxCodebook.WhiteRunCodes, 12);
    private static readonly ushort[] BlackRunLookup = BuildRunLookup(CcittFaxCodebook.BlackRunCodes, 13);

    private static ushort[] BuildRunLookup(CcittCode[] runCodes, int lookaheadBitCount)
    {
        var lookup = new ushort[1 << lookaheadBitCount];
        foreach (var code in runCodes)
        {
#if NET8_0_OR_GREATER
            Array.Fill(lookup, checked((ushort)((code.Run << 4) | code.Length)), code.Bits << (lookaheadBitCount - code.Length), 1 << (lookaheadBitCount - code.Length));
#else
            lookup.AsSpan(code.Bits << (lookaheadBitCount - code.Length), 1 << (lookaheadBitCount - code.Length)).Fill(checked((ushort)((code.Run << 4) | code.Length)));
#endif
        }
        return lookup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeRunLength<T>(ref CcittFaxCompactBitReader bitReader, bool isWhiteRun, int remainingPixels)
        where T : struct, ICcittRowPolicy
    {
        var lookup = isWhiteRun ? WhiteRunLookup : BlackRunLookup;
        int lookaheadBitCount = isWhiteRun ? 12 : 13;
        int accumulatedRunLength = 0;
        int runLength;
        do
        {
            int lookupEntry = lookup[bitReader.PeekBits<T>(lookaheadBitCount)];
            if (lookupEntry == 0)
                throw new InvalidDataException("Invalid CCITT Huffman code.");
            bitReader.ConsumeBits(lookupEntry & 15);
            runLength = lookupEntry >> 4;
            if (runLength > remainingPixels - accumulatedRunLength)
                throw new InvalidDataException("CCITT run exceeds row bounds.");
            accumulatedRunLength += runLength;
        }
        while (runLength >= 64);
        return accumulatedRunLength;
    }

    // Ordinary T.4/T.6 2D operations fit in seven bits. Each code fills all entries beginning
    // with that code, replacing bit-by-bit tree traversal with one lookup. Unmapped prefixes
    // use the bit-by-bit reader, which can trigger compatibility decoding.
    private static readonly int[] ModeLookup = BuildModeLookup();
    // Bits 0..2 store the code length. The remaining bits store a signed vertical offset
    // (-3..3), HorizontalMode (two runs), or PassMode (skip two reference transitions).
    private static int[] BuildModeLookup()
    {
        var lookup = new int[128];
        foreach (var (codeBits, codeBitCount, operation) in new[]
        {
            (1, 1, 0),
            (3, 3, 1),
            (2, 3, -1),
            (1, 3, HorizontalMode),
            (1, 4, PassMode),
            (3, 6, 2),
            (2, 6, -2),
            (3, 7, 3),
            (2, 7, -3)
        })
        {
#if NET8_0_OR_GREATER
            Array.Fill(lookup, (operation << 3) | codeBitCount, codeBits << (7 - codeBitCount), 1 << (7 - codeBitCount));
#else
            lookup.AsSpan(codeBits << (7 - codeBitCount), 1 << (7 - codeBitCount)).Fill((operation << 3) | codeBitCount);
#endif
        }
        return lookup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeMode<T>(ref CcittFaxCompactBitReader bitReader)
        where T : struct, ICcittRowPolicy
    {
        int lookupEntry = ModeLookup[bitReader.PeekBits<T>(7)];
        if (lookupEntry == 0)
            return bitReader.ReadModeBitByBit<T>();
        bitReader.ConsumeBits(lookupEntry & 7);
        return lookupEntry >> 3;
    }

    // Four value-type policies specialize two choices: Group 4 versus other row formats,
    // and black pixels encoded as one versus zero. The JIT can fold their constant getters.
    // .NET 8+ uses static interface members; older targets use constrained instance calls
    // on a default value-type instance, without boxing. The decoding rules are the same.
#if NET8_0_OR_GREATER
    private readonly struct Group4BlackIsOnePolicy : ICcittRowPolicy
    {
        public static bool IsGroup4 => true;
        public static bool BlackIsOne => true;
    }

    private readonly struct Group4BlackIsZeroPolicy : ICcittRowPolicy
    {
        public static bool IsGroup4 => true;
        public static bool BlackIsOne => false;
    }

    private readonly struct GeneralBlackIsOnePolicy : ICcittRowPolicy
    {
        public static bool IsGroup4 => false;
        public static bool BlackIsOne => true;
    }

    private readonly struct GeneralBlackIsZeroPolicy : ICcittRowPolicy
    {
        public static bool IsGroup4 => false;
        public static bool BlackIsOne => false;
    }

#else
    private readonly struct Group4BlackIsOnePolicy : ICcittRowPolicy
    {
        public bool IsGroup4 => true;
        public bool BlackIsOne => true;
    }

    private readonly struct Group4BlackIsZeroPolicy : ICcittRowPolicy
    {
        public bool IsGroup4 => true;
        public bool BlackIsOne => false;
    }

    private readonly struct GeneralBlackIsOnePolicy : ICcittRowPolicy
    {
        public bool IsGroup4 => false;
        public bool BlackIsOne => true;
    }

    private readonly struct GeneralBlackIsZeroPolicy : ICcittRowPolicy
    {
        public bool IsGroup4 => false;
        public bool BlackIsOne => false;
    }

#endif
    // A 16-bit entry packs two terminating pixel counts into bits 0..5 and 6..11,
    // and their combined code length into bits 12..15. Only pairs of at most 12 bits fit
    // this lookup; a zero entry means decode the runs individually. One table starts
    // with white, the other with black. Their entry payloads total 16 KiB.
    private static class RunPairLookup
    {
        internal static readonly ushort[] WhiteFirstPairs = BuildRunPairLookup(true);
        internal static readonly ushort[] BlackFirstPairs = BuildRunPairLookup(false);

        private static ushort[] BuildRunPairLookup(bool firstRunIsWhite)
        {
            var lookup = new ushort[4096];
            foreach (var firstCode in firstRunIsWhite ? CcittFaxCodebook.WhiteRunCodes : CcittFaxCodebook.BlackRunCodes)
            {
                foreach (var secondCode in firstRunIsWhite ? CcittFaxCodebook.BlackRunCodes : CcittFaxCodebook.WhiteRunCodes)
                {
                    int combinedBitCount = firstCode.Length + secondCode.Length;
                    if (firstCode.Run >= 64 || secondCode.Run >= 64 || combinedBitCount > 12)
                        continue;
                    ushort lookupEntry = checked((ushort)(firstCode.Run | (secondCode.Run << 6) | (combinedBitCount << 12)));
                    int combinedCodeBits = (firstCode.Bits << secondCode.Length) | secondCode.Bits;
#if NET8_0_OR_GREATER
                    Array.Fill(lookup, lookupEntry, combinedCodeBits << (12 - combinedBitCount), 1 << (12 - combinedBitCount));
#else
                    lookup.AsSpan(combinedCodeBits << (12 - combinedBitCount), 1 << (12 - combinedBitCount)).Fill(lookupEntry);
#endif
                }
            }

            return lookup;
        }
    }

    // The caller must validate dimensions, output capacity and the filter allocation budget first.
    // This path requires UInt16 positions within the row that never move backwards.
    // A wider row returns false without touching output. Invalid or truncated codes clear
    // the partial output and return false. Decode then retries from the original input with
    // signed positions, where strict/lenient parsing decides how to handle the data.
    internal static bool TryDecode(
        ReadOnlySpan<byte> compressedInput,
        byte[] decodedBitmap,
        int columns,
        int rowCount,
        CcittFaxCompressionType compressionType,
        bool encodedByteAlign,
        bool blackIsOne)
    {
        if (columns > ushort.MaxValue)
            return false;
        try
        {
            if (compressionType == CcittFaxCompressionType.Group4_2D)
            {
                if (blackIsOne)
                    DecodeCompactRows<Group4BlackIsOnePolicy>(compressedInput, decodedBitmap, columns, rowCount, compressionType, encodedByteAlign);
                else
                    DecodeCompactRows<Group4BlackIsZeroPolicy>(compressedInput, decodedBitmap, columns, rowCount, compressionType, encodedByteAlign);
            }
            else
            {
                if (blackIsOne)
                    DecodeCompactRows<GeneralBlackIsOnePolicy>(compressedInput, decodedBitmap, columns, rowCount, compressionType, encodedByteAlign);
                else
                    DecodeCompactRows<GeneralBlackIsZeroPolicy>(compressedInput, decodedBitmap, columns, rowCount, compressionType, encodedByteAlign);
            }

            return true;
        }
        catch (InvalidDataException)
        {
            decodedBitmap.AsSpan().Clear();
            return false;
        }
    }

    // Paint black pixels from start (inclusive) to end (exclusive). Callers initialize the row
    // to white first. Edge masks preserve pixels outside the interval, including row padding.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PaintBlackInterval<T>(Span<byte> rowPixels, int startPosition, int endPosition)
        where T : struct, ICcittRowPolicy
    {
        if (endPosition <= startPosition)
            return;
        int firstByteIndex = startPosition >> 3;
        int lastByteIndex = (endPosition - 1) >> 3;
        int firstByteMask = 255 >> (startPosition & 7);
        int lastByteMask = 255 << (7 - ((endPosition - 1) & 7));
        if (firstByteIndex == lastByteIndex)
        {
            byte pixelMask = (byte)(firstByteMask & lastByteMask);
#if NET8_0_OR_GREATER
            rowPixels[firstByteIndex] = T.BlackIsOne ? (byte)(rowPixels[firstByteIndex] | pixelMask) : (byte)(rowPixels[firstByteIndex] & ~pixelMask);
#else
            rowPixels[firstByteIndex] = default(T).BlackIsOne ? (byte)(rowPixels[firstByteIndex] | pixelMask) : (byte)(rowPixels[firstByteIndex] & ~pixelMask);
#endif
            return;
        }

#if NET8_0_OR_GREATER
        rowPixels[firstByteIndex] = T.BlackIsOne ? (byte)(rowPixels[firstByteIndex] | firstByteMask) : (byte)(rowPixels[firstByteIndex] & ~firstByteMask);
#else
        rowPixels[firstByteIndex] = default(T).BlackIsOne ? (byte)(rowPixels[firstByteIndex] | firstByteMask) : (byte)(rowPixels[firstByteIndex] & ~firstByteMask);
#endif
#if NET8_0_OR_GREATER
        rowPixels.Slice(firstByteIndex + 1, lastByteIndex - firstByteIndex - 1).Fill(T.BlackIsOne ? (byte)255 : (byte)0);
#else
        rowPixels.Slice(firstByteIndex + 1, lastByteIndex - firstByteIndex - 1).Fill(default(T).BlackIsOne ? (byte)255 : (byte)0);
#endif
#if NET8_0_OR_GREATER
        rowPixels[lastByteIndex] = T.BlackIsOne ? (byte)(rowPixels[lastByteIndex] | lastByteMask) : (byte)(rowPixels[lastByteIndex] & ~lastByteMask);
#else
        rowPixels[lastByteIndex] = default(T).BlackIsOne ? (byte)(rowPixels[lastByteIndex] | lastByteMask) : (byte)(rowPixels[lastByteIndex] & ~lastByteMask);
#endif
    }

    private static void DecodeCompactRows<T>(
        ReadOnlySpan<byte> compressedInput,
        byte[] decodedBitmap,
        int columns,
        int rowCount,
        CcittFaxCompressionType compressionType,
        bool encodedByteAlign)
        where T : struct, ICcittRowPolicy
    {
        int rowByteCount = (columns + 7) / 8;
        // Reuse two transition arrays: the previous row is the 2D reference, the current
        // row records newly decoded color changes. Only entries below each row count are valid.
        // Equal positions represent zero-length runs and must remain in their original order.
        var referenceTransitions = new ushort[columns + 2];
        var currentTransitions = new ushort[columns + 2];
        int referenceTransitionCount = 0;
        var bitReader = new CcittFaxCompactBitReader(compressedInput);
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var rowPixels = decodedBitmap.AsSpan(rowIndex * rowByteCount, rowByteCount);
#if NET8_0_OR_GREATER
            rowPixels.Fill(T.BlackIsOne ? (byte)0 : (byte)255);
#else
            rowPixels.Fill(default(T).BlackIsOne ? (byte)0 : (byte)255);
#endif
            bool isOneDimensional;
#if NET8_0_OR_GREATER
            if (T.IsGroup4)
#else
            if (default(T).IsGroup4)
#endif
            {
                if (encodedByteAlign)
                    bitReader.AlignToByteBoundary();
                isOneDimensional = false;
            }
            else
                isOneDimensional = bitReader.ReadRowIsOneDimensional<T>(compressionType, encodedByteAlign);
            bool isWhiteRun = true;
            int pixelPosition = 0;
            int transitionCount = 0;
            int referenceTransitionIndex = 0;
            int operationCount = 0;
            int whiteReferenceIndex = 0;
            int blackReferenceIndex = 1;
            int nextModeEntry = 0;
            bool hasNextModeEntry = false;
            while (pixelPosition < columns)
            {
                // A zero-length run does not advance pixelPosition. Limit the number of operations too,
                // so repeated empty runs cannot keep the compact loop busy indefinitely.
                if (++operationCount > 2L * columns + 4)
                    throw new InvalidDataException("CCITT row does not progress.");
                int operation;
                if (isOneDimensional)
                    operation = 0;
                else if (hasNextModeEntry)
                {
                    hasNextModeEntry = false;
                    if (nextModeEntry == 0)
                        operation = bitReader.ReadModeBitByBit<T>();
                    else
                    {
                        bitReader.ConsumeBits(nextModeEntry & 7);
                        operation = nextModeEntry >> 3;
                    }
                }
                else
                    operation = DecodeMode<T>(ref bitReader);
                if (isOneDimensional || operation == HorizontalMode)
                {
                    uint packedRunPair = (isWhiteRun ? RunPairLookup.WhiteFirstPairs : RunPairLookup.BlackFirstPairs)[bitReader.PeekBits<T>(12)];
                    int firstRunLength = (int)(packedRunPair & 63);
                    int secondRunLength = (int)((packedRunPair >> 6) & 63);
                    // In 1D, a first run that completes the row must not consume a second run
                    // from the following row. A horizontal 2D operation always contains two runs.
                    if (packedRunPair != 0
                        && firstRunLength + secondRunLength <= columns - pixelPosition
                        && (!isOneDimensional || firstRunLength < columns - pixelPosition))
                    {
                        if (currentTransitions.Length - transitionCount < 2)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        bitReader.ConsumeBits((int)(packedRunPair >> 12));
                        int firstRunEnd = pixelPosition + firstRunLength;
                        int secondRunEnd = firstRunEnd + secondRunLength;
                        if (!isOneDimensional)
                        {
                            // Peek the next operation before painting to overlap independent work.
                            // Its bits are consumed only at the next loop iteration.
                            nextModeEntry = ModeLookup[bitReader.PeekBits<T>(7)];
                            hasNextModeEntry = true;
                        }

                        PaintBlackInterval<T>(rowPixels, isWhiteRun ? firstRunEnd : pixelPosition, isWhiteRun ? secondRunEnd : firstRunEnd);
                        currentTransitions[transitionCount++] = checked((ushort)firstRunEnd);
                        currentTransitions[transitionCount++] = checked((ushort)secondRunEnd);
                        pixelPosition = secondRunEnd;
                        continue;
                    }

                    if (!isOneDimensional)
                    {
                        int firstRunEnd = pixelPosition + DecodeRunLength<T>(ref bitReader, isWhiteRun, columns - pixelPosition);
                        if (transitionCount == currentTransitions.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        currentTransitions[transitionCount++] = checked((ushort)firstRunEnd);
                        int secondRunEnd = firstRunEnd + DecodeRunLength<T>(ref bitReader, !isWhiteRun, columns - firstRunEnd);
                        nextModeEntry = ModeLookup[bitReader.PeekBits<T>(7)];
                        hasNextModeEntry = true;
                        PaintBlackInterval<T>(rowPixels, isWhiteRun ? firstRunEnd : pixelPosition, isWhiteRun ? secondRunEnd : firstRunEnd);
                        if (transitionCount == currentTransitions.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        currentTransitions[transitionCount++] = checked((ushort)secondRunEnd);
                        pixelPosition = secondRunEnd;
                        continue;
                    }

                    int nextPosition = pixelPosition + DecodeRunLength<T>(ref bitReader, isWhiteRun, columns - pixelPosition);
                    if (!isWhiteRun)
                        PaintBlackInterval<T>(rowPixels, pixelPosition, nextPosition);
                    if (transitionCount == currentTransitions.Length)
                        throw new InvalidDataException("Too many CCITT transitions.");
                    currentTransitions[transitionCount++] = checked((ushort)nextPosition);
                    pixelPosition = nextPosition;
                    isWhiteRun = !isWhiteRun;
                }
                else
                {
                    nextModeEntry = ModeLookup[bitReader.PeekBits<T>(7)];
                    hasNextModeEntry = true;

                    // Even and odd cursors track the next reference transition for each color.
                    // They only advance because pixelPosition never moves backwards. At the row start,
                    // retain an initial zero-position transition; it changes the color.
                    referenceTransitionIndex = isWhiteRun ? whiteReferenceIndex : blackReferenceIndex;
                    while (referenceTransitionIndex < referenceTransitionCount && pixelPosition != 0 && referenceTransitions[referenceTransitionIndex] <= pixelPosition)
                        referenceTransitionIndex += 2;
                    if (isWhiteRun)
                        whiteReferenceIndex = referenceTransitionIndex;
                    else
                        blackReferenceIndex = referenceTransitionIndex;
                    // The signed path can wrap this pass to the first reference transition
                    // when no later transition exists. Retry there instead of substituting width;
                    // its position validation applies the requested strict/lenient policy.
                    if (operation == PassMode
                        && pixelPosition != 0
                        && referenceTransitionCount != 0
                        && referenceTransitionIndex >= referenceTransitionCount)
                        throw new InvalidDataException("CCITT pass requires compatibility decoding.");
                    // The first reference transition is b1 in the T.4/T.6 notation.
                    int referencePosition = referenceTransitionIndex < referenceTransitionCount
                        ? referenceTransitions[referenceTransitionIndex]
                        : columns;
                    int nextPosition = operation == PassMode
                        ? (referenceTransitionIndex + 1 < referenceTransitionCount ? referenceTransitions[referenceTransitionIndex + 1] : columns)
                        : referencePosition + operation;
                    if (nextPosition < pixelPosition || nextPosition > columns)
                        throw new InvalidDataException("Invalid CCITT vertical/pass position.");
                    if (!isWhiteRun)
                        PaintBlackInterval<T>(rowPixels, pixelPosition, nextPosition);
                    pixelPosition = nextPosition;
                    if (operation != PassMode)
                    {
                        if (transitionCount == currentTransitions.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        currentTransitions[transitionCount++] = checked((ushort)pixelPosition);
                        isWhiteRun = !isWhiteRun;
                    }
                }
            }

            (referenceTransitions, currentTransitions) = (currentTransitions, referenceTransitions);
            referenceTransitionCount = transitionCount;
        }
    }
}
