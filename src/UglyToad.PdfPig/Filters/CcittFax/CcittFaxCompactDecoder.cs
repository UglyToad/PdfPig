using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace UglyToad.PdfPig.Filters.CcittFax;
/// <summary>
/// Decodes CCITT rows using compact transitions and direct output polarity.
/// Widths above 65,535 and exceptional input use signed transition positions to preserve compatibility.
/// </summary>
// Original C# implementation under the repository Apache-2.0 license.
// The algorithm represents rows as alternating color-change positions and decodes standard T.4/T.6
// codes through flat lookup tables. Packing two short terminating runs into one ushort lets a single
// lookup decode a horizontal pair; longer runs retain ordinary bounded Huffman decoding.
// Painting black intervals directly in the requested polarity avoids a full-bitmap inversion pass.
// Every output byte is initialized, including unused row padding.
// Next-operation preloading was inspired by the MIT-licensed libdeflate decompression fast loop:
// https://github.com/ebiggers/libdeflate/blob/master/lib/decompress_template.h
// The next CCITT mode is peeked before painting and consumed on the next iteration, allowing
// independent bit decoding and pixel writes to overlap. This is an original adaptation of the idea;
// no libdeflate implementation code or lookup tables were copied.
internal static partial class CcittFaxCompactDecoder
{
    // A run entry stores the code length in bits 0..3 and the run length in bits 4..15.
    // Zero is an invalid prefix. Terminating runs are 0..63; makeup runs accumulate until a terminator.
    private static readonly ushort[] White = Build(CcittFaxCodebook.White, 12), Black = Build(CcittFaxCodebook.Black, 13);
    private static ushort[] Build(CcittCode[] codes, int lookahead)
    {
        var table = new ushort[1 << lookahead];
        foreach (var c in codes)
#if NET8_0_OR_GREATER
            Array.Fill(table, checked((ushort)((c.Run << 4) | c.Length)), c.Bits << (lookahead - c.Length), 1 << (lookahead - c.Length));
#else
            table.AsSpan(c.Bits << (lookahead - c.Length), 1 << (lookahead - c.Length)).Fill(checked((ushort)((c.Run << 4) | c.Length)));
#endif
        return table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Run<T>(ref CcittFaxCompactBitReader bits, bool white, int remaining)
        where T : struct, ICcittCompactFeatures
    {
        var table = white ? White : Black;
        int lookahead = white ? 12 : 13;
        int total = 0, run;
        do
        {
            int entry = table[bits.Peek<T>(lookahead)];
            if (entry == 0)
                throw new InvalidDataException("Invalid CCITT Huffman code.");
            bits.Drop(entry & 15);
            run = entry >> 4;
            if (run > remaining - total)
                throw new InvalidDataException("CCITT run exceeds row bounds.");
            total += run;
        }
        while (run >= 64);
        return total;
    }

    // Standard T.4/T.6 vertical, horizontal and pass codes fit in seven bits.
    // Expand each prefix over its possible suffixes to replace a bit-by-bit tree traversal.
    // Invalid prefixes fall back without consuming bits to preserve failure semantics.
    private static readonly int[] Modes = BuildModes();
    // Mode entries store the consumed length in bits 0..2 and the signed operation above it.
    // Operations -3..3 are vertical offsets; 100 is horizontal and 101 is pass.
    private static int[] BuildModes()
    {
        var table = new int[128];
        foreach (var (code, length, mode) in new[]
        {
            (1, 1, 0),
            (3, 3, 1),
            (2, 3, -1),
            (1, 3, 100),
            (1, 4, 101),
            (3, 6, 2),
            (2, 6, -2),
            (3, 7, 3),
            (2, 7, -3)
        })
#if NET8_0_OR_GREATER
            Array.Fill(table, (mode << 3) | length, code << (7 - length), 1 << (7 - length));
#else
            table.AsSpan(code << (7 - length), 1 << (7 - length)).Fill((mode << 3) | length);
#endif
        return table;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Mode<T>(ref CcittFaxCompactBitReader bits)
        where T : struct, ICcittCompactFeatures
    {
        int entry = Modes[bits.Peek<T>(7)];
        if (entry == 0)
            return bits.Mode<T>();
        bits.Drop(entry & 7);
        return entry >> 3;
    }

    // The four value-type policies specialize Group 4/general decoding and output polarity.
    // Constant getters let the JIT remove feature branches. Older targets use constrained instance calls.
#if NET8_0_OR_GREATER
    private readonly struct Features6OneG4 : ICcittCompactFeatures
    {
        public static bool Group4 => true;
        public static bool Table16 => true;
        public static bool Preload => true;
        public static bool Overlap => true;
        public static bool BlackOne => true;
        public static bool WholeBitmap => false;
    }

    private readonly struct Features6ZeroG4 : ICcittCompactFeatures
    {
        public static bool Group4 => true;
        public static bool Table16 => true;
        public static bool Preload => true;
        public static bool Overlap => true;
        public static bool BlackOne => false;
        public static bool WholeBitmap => false;
    }

    private readonly struct Features6OneGeneral : ICcittCompactFeatures
    {
        public static bool Group4 => false;
        public static bool Table16 => true;
        public static bool Preload => true;
        public static bool Overlap => true;
        public static bool BlackOne => true;
        public static bool WholeBitmap => false;
    }

    private readonly struct Features6ZeroGeneral : ICcittCompactFeatures
    {
        public static bool Group4 => false;
        public static bool Table16 => true;
        public static bool Preload => true;
        public static bool Overlap => true;
        public static bool BlackOne => false;
        public static bool WholeBitmap => false;
    }

#else
    private readonly struct Features6OneG4 : ICcittCompactFeatures
    {
        public bool Group4 => true;
        public bool Table16 => true;
        public bool Preload => true;
        public bool Overlap => true;
        public bool BlackOne => true;
        public bool WholeBitmap => false;
    }

    private readonly struct Features6ZeroG4 : ICcittCompactFeatures
    {
        public bool Group4 => true;
        public bool Table16 => true;
        public bool Preload => true;
        public bool Overlap => true;
        public bool BlackOne => false;
        public bool WholeBitmap => false;
    }

    private readonly struct Features6OneGeneral : ICcittCompactFeatures
    {
        public bool Group4 => false;
        public bool Table16 => true;
        public bool Preload => true;
        public bool Overlap => true;
        public bool BlackOne => true;
        public bool WholeBitmap => false;
    }

    private readonly struct Features6ZeroGeneral : ICcittCompactFeatures
    {
        public bool Group4 => false;
        public bool Table16 => true;
        public bool Preload => true;
        public bool Overlap => true;
        public bool BlackOne => false;
        public bool WholeBitmap => false;
    }

#endif
    // Two terminating runs fit in one ushort: first [0..5], second [6..11], consumed bits [12..15].
    // Only pairs consuming at most 12 bits are stored; zero requests ordinary run decoding.
    // Two 4096-entry tables occupy 16 KiB and are shared by both row implementations.
    private static class Pairs16
    {
        internal static readonly ushort[] WhitePairs = BuildPairs(true), BlackPairs = BuildPairs(false);
        private static ushort[] BuildPairs(bool white)
        {
            var table = new ushort[4096];
            foreach (var first in white ? CcittFaxCodebook.White : CcittFaxCodebook.Black)
                foreach (var second in white ? CcittFaxCodebook.Black : CcittFaxCodebook.White)
                {
                    int length = first.Length + second.Length;
                    if (first.Run >= 64 || second.Run >= 64 || length > 12)
                        continue;
                    ushort entry = checked((ushort)(first.Run | (second.Run << 6) | (length << 12)));
                    int prefix = (first.Bits << second.Length) | second.Bits;
#if NET8_0_OR_GREATER
                    Array.Fill(table, entry, prefix << (12 - length), 1 << (12 - length));
#else
                    table.AsSpan(prefix << (12 - length), 1 << (12 - length)).Fill(entry);
#endif
                }

            return table;
        }
    }

    // Call after the filter has validated its unchanged total bitmap/work-buffer budget.
    // Strict compact probe. Decode handles wide images and exceptional input internally.
    internal static bool TryDecode(ReadOnlySpan<byte> input, byte[] output, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool blackIsOne)
    {
        if (width > ushort.MaxValue)
            return false;
        try
        {
            if (mode == CcittFaxCompressionType.Group4_2D)
            {
                if (blackIsOne)
                    DecodeCore<Features6OneG4>(input, output, width, rows, mode, aligned);
                else
                    DecodeCore<Features6ZeroG4>(input, output, width, rows, mode, aligned);
            }
            else
            {
                if (blackIsOne)
                    DecodeCore<Features6OneGeneral>(input, output, width, rows, mode, aligned);
                else
                    DecodeCore<Features6ZeroGeneral>(input, output, width, rows, mode, aligned);
            }

            return true;
        }
        catch (InvalidDataException)
        {
            output.AsSpan().Clear();
            return false;
        }
    }

    private static int Advance(int start, int amount, int width)
    {
        if (amount < 0 || amount > width - start)
            throw new InvalidDataException("CCITT run exceeds row bounds.");
        return start + amount;
    }

    // Paint the half-open black interval [start, end). Rows start white in the requested polarity,
    // so only black pixels are changed; unused bits in the last byte remain white.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Paint<T>(Span<byte> row, int start, int end)
        where T : struct, ICcittCompactFeatures
    {
        if (end <= start)
            return;
        int first = start >> 3, last = (end - 1) >> 3;
        int firstMask = 255 >> (start & 7), lastMask = 255 << (7 - ((end - 1) & 7));
        if (first == last)
        {
            byte mask = (byte)(firstMask & lastMask);
#if NET8_0_OR_GREATER
            row[first] = T.BlackOne ? (byte)(row[first] | mask) : (byte)(row[first] & ~mask);
#else
            row[first] = default(T).BlackOne ? (byte)(row[first] | mask) : (byte)(row[first] & ~mask);
#endif
            return;
        }

#if NET8_0_OR_GREATER
        row[first] = T.BlackOne ? (byte)(row[first] | firstMask) : (byte)(row[first] & ~firstMask);
#else
        row[first] = default(T).BlackOne ? (byte)(row[first] | firstMask) : (byte)(row[first] & ~firstMask);
#endif
#if NET8_0_OR_GREATER
        row.Slice(first + 1, last - first - 1).Fill(T.BlackOne ? (byte)255 : (byte)0);
#else
        row.Slice(first + 1, last - first - 1).Fill(default(T).BlackOne ? (byte)255 : (byte)0);
#endif
#if NET8_0_OR_GREATER
        row[last] = T.BlackOne ? (byte)(row[last] | lastMask) : (byte)(row[last] & ~lastMask);
#else
        row[last] = default(T).BlackOne ? (byte)(row[last] | lastMask) : (byte)(row[last] & ~lastMask);
#endif
    }

    private static void DecodeCore<T>(ReadOnlySpan<byte> input, byte[] output, int width, int rows, CcittFaxCompressionType mode, bool aligned)
        where T : struct, ICcittCompactFeatures
    {
#if NET8_0_OR_GREATER
        if (T.WholeBitmap)
#else
        if (default(T).WholeBitmap)
#endif
#if NET8_0_OR_GREATER
            output.AsSpan().Fill(T.BlackOne ? (byte)0 : (byte)255);
#else
            output.AsSpan().Fill(default(T).BlackOne ? (byte)0 : (byte)255);
#endif
        int stride = (width + 7) / 8;
        // Alternating transition positions describe each row without storing a second bitmap.
        // Only entries below the associated count are initialized; duplicate positions are meaningful.
        var previous = new ushort[width + 2];
        var current = new ushort[width + 2];
        int previousCount = 0;
        var bits = new CcittFaxCompactBitReader(input);
        for (int y = 0; y < rows; y++)
        {
            var row = output.AsSpan(y * stride, stride);
#if NET8_0_OR_GREATER
            if (!T.WholeBitmap)
#else
            if (!default(T).WholeBitmap)
#endif
#if NET8_0_OR_GREATER
                row.Fill(T.BlackOne ? (byte)0 : (byte)255);
#else
                row.Fill(default(T).BlackOne ? (byte)0 : (byte)255);
#endif
            bool oneD;
#if NET8_0_OR_GREATER
            if (T.Group4)
#else
            if (default(T).Group4)
#endif
            {
                if (aligned)
                    bits.Align();
                oneD = false;
            }
            else
                oneD = bits.OneDimensional<T>(mode, aligned);
            bool white = true;
            int x = 0, count = 0, cursor = 0, operations = 0;
            int whiteCursor = 0, blackCursor = 1;
            int prepared = 0;
            bool hasPrepared = false;
            while (x < width)
            {
                // Zero-length runs can preserve x. Bound operations as well as output size so
                // malformed rows cannot keep decoding forever without advancing the pixel position.
                if (++operations > 2L * width + 4)
                    throw new InvalidDataException("CCITT row does not progress.");
                int operation;
                if (oneD)
                    operation = 0;
#if NET8_0_OR_GREATER
                else if (T.Preload && hasPrepared)
#else
                else if (default(T).Preload && hasPrepared)
#endif
                {
                    hasPrepared = false;
                    if (prepared == 0)
                        operation = bits.Mode<T>();
                    else
                    {
                        bits.Drop(prepared & 7);
                        operation = prepared >> 3;
                    }
                }
                else
                    operation = Mode<T>(ref bits);
                if (oneD || operation == 100)
                {
                    uint pair = (white ? Pairs16.WhitePairs : Pairs16.BlackPairs)[bits.Peek<T>(12)];
                    int first = (int)(pair & 63), second = (int)((pair >> 6) & 63);
                    // In 1D, a first run that completes the row must not consume a second run
                    // from the following row. A horizontal 2D operation always contains two runs.
                    if (pair != 0 && first + second <= width - x && (!oneD || first < width - x))
                    {
                        if (current.Length - count < 2)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        bits.Drop((int)(pair >> 12));
                        int middle = x + first, end = middle + second;
#if NET8_0_OR_GREATER
                        if (T.Preload && !oneD)
#else
                        if (default(T).Preload && !oneD)
#endif
                        {
                            // Peek the next operation before painting to overlap independent work.
                            // Its bits are consumed only at the next loop iteration.
                            prepared = Modes[bits.Peek<T>(7)];
                            hasPrepared = true;
                        }

                        Paint<T>(row, white ? middle : x, white ? end : middle);
                        current[count++] = checked((ushort)middle);
                        current[count++] = checked((ushort)end);
                        x = end;
                        continue;
                    }

#if NET8_0_OR_GREATER
                    if (T.Preload && !oneD)
#else
                    if (default(T).Preload && !oneD)
#endif
                    {
                        int middle = Advance(x, Run<T>(ref bits, white, width - x), width);
                        if (count == current.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        current[count++] = checked((ushort)middle);
                        int end = Advance(middle, Run<T>(ref bits, !white, width - middle), width);
                        prepared = Modes[bits.Peek<T>(7)];
                        hasPrepared = true;
                        Paint<T>(row, white ? middle : x, white ? end : middle);
                        if (count == current.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        current[count++] = checked((ushort)end);
                        x = end;
                        continue;
                    }

                    int next = Advance(x, Run<T>(ref bits, white, width - x), width);
                    if (!white)
                        Paint<T>(row, x, next);
                    if (count == current.Length)
                        throw new InvalidDataException("Too many CCITT transitions.");
                    current[count++] = checked((ushort)next);
                    x = next;
                    white = !white;
                    if (!oneD)
                    {
                        next = Advance(x, Run<T>(ref bits, white, width - x), width);
                        if (!white)
                            Paint<T>(row, x, next);
                        if (count == current.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        current[count++] = checked((ushort)next);
                        x = next;
                        white = !white;
                    }
                }
                else
                {
#if NET8_0_OR_GREATER
                    if (T.Preload)
#else
                    if (default(T).Preload)
#endif
                    {
                        prepared = Modes[bits.Peek<T>(7)];
                        hasPrepared = true;
                    }

                    // Each parity cursor is monotone because a0 never moves backwards.
                    // Keep duplicate and initial-zero transitions; do not skip at a0 == 0.
                    cursor = white ? whiteCursor : blackCursor;
                    while (cursor < previousCount && x != 0 && previous[cursor] <= x)
                        cursor += 2;
                    if (white)
                        whiteCursor = cursor;
                    else
                        blackCursor = cursor;
                    // The compatibility decoder treats a pass beyond the last reference
                    // transition as a wrap to its first transition. Preserve its strict/lenient
                    // result through the shared full-width row path.
                    if (operation == 101 && x != 0 && previousCount != 0 && cursor >= previousCount)
                        throw new InvalidDataException("CCITT pass requires compatibility decoding.");
                    int b1 = cursor < previousCount ? previous[cursor] : width;
                    int next = operation == 101 ? (cursor + 1 < previousCount ? previous[cursor + 1] : width) : b1 + operation;
                    if (next < x || next > width)
                        throw new InvalidDataException("Invalid CCITT vertical/pass position.");
                    if (!white)
                        Paint<T>(row, x, next);
                    x = next;
                    if (operation != 101)
                    {
                        if (count == current.Length)
                            throw new InvalidDataException("Too many CCITT transitions.");
                        current[count++] = checked((ushort)x);
                        white = !white;
                    }
                }
            }

            (previous, current) = (current, previous);
            previousCount = count;
        }
    }
}
