namespace UglyToad.PdfPig.Filters.CcittFax
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using UglyToad.PdfPig.Fonts;

    /// <summary>Lets the JIT specialize row decoding for Group 4 and output polarity.</summary>
    /// <remarks>Value-type configurations avoid boxing; older targets use constrained instance properties.</remarks>
    internal interface ICcittFaxDecodingConfiguration
    {
#if NET8_0_OR_GREATER
        static abstract bool IsGroup4 { get; }
        static abstract bool BlackIsOne { get; }
#else
        bool IsGroup4 { get; }

        bool BlackIsOne { get; }

#endif
    }

    /// <summary>Decodes MH, Group 3 and Group 4 CCITT data into an MSB-first packed bitmap.</summary>
    /// <remarks>
    /// <para>1D rows alternate white and black run lengths, starting with white (possibly zero pixels).
    /// 2D rows describe changes from the preceding row, initially all white. Horizontal mode reads two
    /// runs; vertical mode places an edge relative to a reference edge; pass mode skips two reference
    /// edges without changing color. Completed run ends become the next row's reference transitions.</para>
    /// <para>The row loop checks termination, reads framing, decodes runs or relative edges, paints black
    /// intervals, then swaps the transition arrays. Prefix tables and short-run pairs replace tree walks;
    /// UInt16 transitions reduce working storage. Painting in the requested polarity avoids inversion.</para>
    /// <para>Height limits stored pixels, not decoding: extra rows update the reference until termination.
    /// The filter owns parameter resolution, buffer limits and legacy retries; this decoder reports the
    /// ending and input consumption. Invalid rows return false; budget failures throw.</para>
    /// <para>Provenance: T.4/T.6 codewords and reference-row rules derive from the Apache-2.0
    /// <see href="https://github.com/UglyToad/PdfPig/blob/bdbc5f47fdbca11542db7ee876426ee601374427/src/UglyToad.PdfPig/Filters/CcittFax/CcittFaxDecoderStream.cs">PdfPig decoder</see>
    /// and its attributed Apache-2.0
    /// <see href="https://github.com/apache/pdfbox/blob/e644c29279e276bde14ce7a33bdeef0cb1001b3e/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxDecoderStream.java">PDFBox decoder</see>.
    /// The MIT <see href="https://github.com/ebiggers/libdeflate/blob/8ae3a19a59173f16e3738d45b54a1cdf05ad12d4/lib/decompress_template.h">libdeflate fast loop</see>
    /// inspired peeking the next mode before painting; no libdeflate code or tables were copied.
    /// Packed run pairs, lookup builders, compact transitions and polarity painting are implemented here.
    /// Bit-reader inspiration is documented on <see cref="CcittFaxBitReader"/>.</para>
    /// </remarks>
    internal static class CcittFaxBitmapDecoder
    {
        private const int HorizontalMode = 100;
        private const int PassMode = 101;
        private const int WhiteRunLookaheadBitCount = 12;
        private const int BlackRunLookaheadBitCount = 13;
        private const int RunCodeLengthBitCount = 4;
        private const int RunCodeLengthMask = (1 << RunCodeLengthBitCount) - 1;
        private const int TerminatingRunLimit = 64;
        private const int ModeLookaheadBitCount = 7;
        private const int ModeCodeLengthBitCount = 3;
        private const int ModeCodeLengthMask = (1 << ModeCodeLengthBitCount) - 1;
        private const int RunPairLookaheadBitCount = 12;
        private const int RunPairLengthBitCount = 6;
        private const int RunPairLengthMask = (1 << RunPairLengthBitCount) - 1;

        private const int RunPairCodeLengthShift = 2 * RunPairLengthBitCount;

        // Bits 0..3 store the consumed code length; bits 4..15 store its pixel count.
        // A zero entry marks an unmapped prefix, not a zero-pixel run. A run uses zero or more
        // makeup codes (multiples of 64), then a terminating code (0..63).
        private static readonly ushort[] WhiteRunLookup =
            BuildRunLookupTable(CcittFaxHuffmanCodes.WhiteRunCodes, WhiteRunLookaheadBitCount);

        private static readonly ushort[] BlackRunLookup =
            BuildRunLookupTable(CcittFaxHuffmanCodes.BlackRunCodes, BlackRunLookaheadBitCount);

        /// <summary>Expands each variable-length Huffman code into all matching lookahead prefixes.</summary>
        /// <remarks>One lookup returns both pixel count and bits to consume, avoiding a tree walk.
        /// White and black tables use 12 and 13 bits respectively to cover their longest codes.</remarks>
        /// <example>
        /// The white terminating code 1000 represents three pixels. With 12-bit lookahead,
        /// indices 1000xxxxxxxx (0x800..0x8FF) all contain (3 &lt;&lt; 4) | 4 = 0x34.
        /// Only the four code bits are consumed; the remaining bits belong to later codes.
        /// </example>
        private static ushort[] BuildRunLookupTable(CcittFaxRunCode[] runCodes, int lookaheadBitCount)
        {
            var lookup = new ushort[1 << lookaheadBitCount];
            foreach (var code in runCodes)
            {
#if NET8_0_OR_GREATER
                Array.Fill(lookup,
                    checked((ushort)((code.RunLength << RunCodeLengthBitCount) | code.CodeBitCount)),
                    code.CodeBits << (lookaheadBitCount - code.CodeBitCount),
                    1 << (lookaheadBitCount - code.CodeBitCount));
#else
                lookup.AsSpan(code.CodeBits << (lookaheadBitCount - code.CodeBitCount),
                        1 << (lookaheadBitCount - code.CodeBitCount))
                    .Fill(checked((ushort)((code.RunLength << RunCodeLengthBitCount) | code.CodeBitCount)));
#endif
            }

            return lookup;
        }

        /// <summary>Adds makeup lengths until a terminating code completes one run of a single color.</summary>
        /// <remarks>A run requires a terminating code (0..63), even when makeup codes already reach the row edge.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DecodeRunLength<T>(ref CcittFaxBitReader bitReader, bool isWhiteRun, int remainingPixels)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            var lookup = isWhiteRun ? WhiteRunLookup : BlackRunLookup;
            int lookaheadBitCount = isWhiteRun ? WhiteRunLookaheadBitCount : BlackRunLookaheadBitCount;
            int accumulatedRunLength = 0;
            int runLength;
            do
            {
                int lookupEntry = lookup[bitReader.PeekBits<T>(lookaheadBitCount)];
                if (lookupEntry == 0)
                {
                    throw new InvalidDataException("Invalid CCITT Huffman code.");
                }

                bitReader.ConsumeBits(lookupEntry & RunCodeLengthMask);
                runLength = lookupEntry >> RunCodeLengthBitCount;
                if (runLength > remainingPixels - accumulatedRunLength)
                {
                    throw new InvalidDataException("CCITT run exceeds row bounds.");
                }

                accumulatedRunLength += runLength;
            } while (runLength >= TerminatingRunLimit);

            return accumulatedRunLength;
        }

        // Seven bits cover every ordinary 2D operation; only special or invalid prefixes
        // need the bit-by-bit reader.
        private static readonly int[] ModeLookup = BuildModeLookupTable();

        // Bits 0..2 store the code length. The remaining bits store a signed vertical offset
        // (-3..3), HorizontalMode (two runs), or PassMode (skip two reference transitions).
        /// <summary>Maps ordinary 2D prefixes to pass, horizontal or signed vertical operations.</summary>
        /// <remarks>Code length and operation share one entry. Special or invalid prefixes remain zero
        /// and go through the bit-by-bit reader, which rejects unsupported extensions.</remarks>
        private static int[] BuildModeLookupTable()
        {
            var lookup = new int[1 << ModeLookaheadBitCount];
            foreach (var (codeBits, codeBitCount, operation)in new[]
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
                     }
                    )
            {
#if NET8_0_OR_GREATER
                Array.Fill(lookup,
                    (operation << ModeCodeLengthBitCount) | codeBitCount,
                    codeBits << (ModeLookaheadBitCount - codeBitCount),
                    1 << (ModeLookaheadBitCount - codeBitCount));
#else
                lookup.AsSpan(codeBits << (ModeLookaheadBitCount - codeBitCount),
                        1 << (ModeLookaheadBitCount - codeBitCount))
                    .Fill((operation << ModeCodeLengthBitCount) | codeBitCount);
#endif
            }

            return lookup;
        }

        /// <summary>Consumes a 2D operation through the prefix table, or examines an unmapped code bit by bit.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DecodeMode<T>(ref CcittFaxBitReader bitReader)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            int lookupEntry = ModeLookup[bitReader.PeekBits<T>(ModeLookaheadBitCount)];
            if (lookupEntry == 0)
            {
                return DecodeModeBitByBit<T>(ref bitReader);
            }

            bitReader.ConsumeBits(lookupEntry & ModeCodeLengthMask);
            return lookupEntry >> ModeCodeLengthBitCount;
        }

        /// <summary>Reads the 2D code tree when a prefix has no ordinary lookup entry.</summary>
        /// <remarks>End markers are valid only between rows. Unsupported extensions reject the attempt.</remarks>
        private static int DecodeModeBitByBit<T>(ref CcittFaxBitReader bitReader)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            // Vertical codes give offsets from a reference-row transition: 1 means zero,
            // 011/010 mean +1/-1. Horizontal (001) reads two runs; pass (0001) skips a reference pair.
            if (bitReader.ReadBits<T>(1) != 0)
            {
                return 0;
            }

            var middleBits = bitReader.ReadBits<T>(2);
            if (middleBits == 1)
            {
                return HorizontalMode;
            }

            if (middleBits >= 2)
            {
                return middleBits == 3 ? 1 : -1;
            }

            if (bitReader.ReadBits<T>(1) != 0)
            {
                return PassMode;
            }

            var tailBits = bitReader.ReadBits<T>(2);
            if (tailBits >= 2)
            {
                return tailBits == 3 ? 2 : -2;
            }

            if (tailBits == 1)
            {
                return bitReader.ReadBits<T>(1) != 0 ? 3 : -3;
            }

            throw new InvalidDataException("Unsupported extension, EOFB or invalid CCITT mode.");
        }

        /// <summary>Specializes Group 4 decoding with black pixels represented by one bits.</summary>
        private readonly struct Group4BlackIsOneConfiguration : ICcittFaxDecodingConfiguration
        {
#if NET8_0_OR_GREATER
            public static bool IsGroup4 => true;
            public static bool BlackIsOne => true;
#else
            public bool IsGroup4 => true;
            public bool BlackIsOne => true;
#endif
        }

        /// <summary>Specializes Group 4 decoding with black pixels represented by zero bits.</summary>
        private readonly struct Group4BlackIsZeroConfiguration : ICcittFaxDecodingConfiguration
        {
#if NET8_0_OR_GREATER
            public static bool IsGroup4 => true;
            public static bool BlackIsOne => false;
#else
            public bool IsGroup4 => true;
            public bool BlackIsOne => false;
#endif
        }

        /// <summary>Selects row framing from the compression type and represents black pixels by one bits.</summary>
        private readonly struct GeneralBlackIsOneConfiguration : ICcittFaxDecodingConfiguration
        {
#if NET8_0_OR_GREATER
            public static bool IsGroup4 => false;
            public static bool BlackIsOne => true;
#else
            public bool IsGroup4 => false;
            public bool BlackIsOne => true;
#endif
        }

        /// <summary>Selects row framing from the compression type and represents black pixels by zero bits.</summary>
        private readonly struct GeneralBlackIsZeroConfiguration : ICcittFaxDecodingConfiguration
        {
#if NET8_0_OR_GREATER
            public static bool IsGroup4 => false;
            public static bool BlackIsOne => false;
#else
            public bool IsGroup4 => false;
            public bool BlackIsOne => false;
#endif
        }

        /// <summary>
        /// Builds two 12-bit prefix tables that decode a pair of short terminating runs in one lookup.
        /// </summary>
        /// <remarks>
        /// One table starts with white and the other with black. Bits 0..5 and 6..11 store the
        /// two terminating pixel counts; bits 12..15 store their combined code length. A zero
        /// entry means decode the runs individually. Only pairs of at most 12 bits fit the lookup.
        /// </remarks>
        private static class RunPairLookup
        {
            internal static readonly ushort[] WhiteFirstPairs = BuildRunPairLookup(true);
            internal static readonly ushort[] BlackFirstPairs = BuildRunPairLookup(false);

            private static ushort[] BuildRunPairLookup(bool firstRunIsWhite)
            {
                var lookup = new ushort[1 << RunPairLookaheadBitCount];
                foreach (var firstCode in firstRunIsWhite
                             ? CcittFaxHuffmanCodes.WhiteRunCodes
                             : CcittFaxHuffmanCodes.BlackRunCodes)
                {
                    foreach (var secondCode in firstRunIsWhite
                                 ? CcittFaxHuffmanCodes.BlackRunCodes
                                 : CcittFaxHuffmanCodes.WhiteRunCodes)
                    {
                        int combinedBitCount = firstCode.CodeBitCount + secondCode.CodeBitCount;
                        if (firstCode.RunLength >= TerminatingRunLimit || secondCode.RunLength >= TerminatingRunLimit ||
                            combinedBitCount > RunPairLookaheadBitCount)
                        {
                            continue;
                        }

                        ushort lookupEntry = checked((ushort)(firstCode.RunLength |
                                                              (secondCode.RunLength << RunPairLengthBitCount) |
                                                              (combinedBitCount << RunPairCodeLengthShift)));
                        int combinedCodeBits = (firstCode.CodeBits << secondCode.CodeBitCount) | secondCode.CodeBits;
#if NET8_0_OR_GREATER
                        Array.Fill(lookup,
                            lookupEntry,
                            combinedCodeBits << (RunPairLookaheadBitCount - combinedBitCount),
                            1 << (RunPairLookaheadBitCount - combinedBitCount));
#else
                        lookup.AsSpan(combinedCodeBits << (RunPairLookaheadBitCount - combinedBitCount),
                            1 << (RunPairLookaheadBitCount - combinedBitCount)).Fill(lookupEntry);
#endif
                    }
                }

                return lookup;
            }
        }

        /// <summary>Attempts direct bitmap decoding with UInt16 color-change positions.</summary>
        /// <param name="compressedInput">CCITT bytes starting at the first encoded row.</param>
        /// <param name="decodedBitmap">Caller-allocated bitmap with space for every requested row.</param>
        /// <param name="columns">Validated positive row width in pixels.</param>
        /// <param name="initialRowCapacity">Fixed number of output rows requested by the caller.</param>
        /// <param name="compressionType">Resolved row framing and coding family.</param>
        /// <param name="encodedByteAlign">Whether each row starts at a byte boundary.</param>
        /// <param name="blackIsOne">Whether black pixels are represented by one bits.</param>
        /// <remarks>This fixed-capacity overload reads the requested rows without requiring an end marker.
        /// The PDF filter uses the options overload to verify termination and account for buffer growth.</remarks>
        /// <returns>True when decoding reached an allowed stopping condition; otherwise false.
        /// A width above UInt16 returns without changing output. Invalid or truncated input clears
        /// output so the caller can restart through the legacy decoder.</returns>
        internal static bool TryDecodeBitmap(ReadOnlySpan<byte> compressedInput,
            byte[] decodedBitmap,
            int columns,
            int initialRowCapacity,
            CcittFaxCompressionType compressionType,
            bool encodedByteAlign,
            bool blackIsOne)
        {
            var options = new CcittFaxDecodeOptions(columns,
                initialRowCapacity,
                compressionType,
                encodedByteAlign,
                blackIsOne);
            return TryDecodeBitmap(compressedInput, ref decodedBitmap, in options, out _);
        }

        /// <summary>Decodes complete rows with bounded growth and reports the encoded stopping condition.</summary>
        /// <remarks>Bitmap capacity and stored image height are independent of PDF Rows. The budget
        /// includes every replaced bitmap awaiting collection, two transition arrays and any scratch row.
        /// Invalid row codes clear the bitmap and return false so a retry sees no partial pixels.
        /// Widths above UInt16 return false without modifying it. Allocation-budget failures throw
        /// rather than triggering a retry that could exceed the same budget.</remarks>
        /// <param name="compressedInput">Encoded bytes for this filter stage; no input bytes are modified.</param>
        /// <param name="decodedBitmap">Initial storage; ref allows a budgeted resize to replace the array.</param>
        /// <param name="options">Resolved row, framing and allocation limits for this attempt.</param>
        /// <param name="result">Complete-row and boundary information, or zero rows and consumption on rejection.
        /// The allocation count is retained even when decoding fails.</param>
        internal static bool TryDecodeBitmap(ReadOnlySpan<byte> compressedInput,
            ref byte[] decodedBitmap,
            in CcittFaxDecodeOptions options,
            out CcittFaxDecodeResult result)
        {
            long allocatedBufferBytes = decodedBitmap.Length + 2L * (options.Columns + 2) * sizeof(ushort);
            if (options.MaximumAllocatedBufferBytes > 0 && allocatedBufferBytes > options.MaximumAllocatedBufferBytes)
            {
                throw new CorruptCompressedDataException("CCITT bitmap decode buffers exceed the allocation budget.");
            }

            result = new CcittFaxDecodeResult(0, 0, CcittFaxDecodeStatus.NotCompleted, allocatedBufferBytes);
            if (options.Columns > ushort.MaxValue)
            {
                return false;
            }

            try
            {
                var bitReader = new CcittFaxBitReader(compressedInput);
                RowDecodeResult rowResult;
                if (options.CompressionType == CcittFaxCompressionType.Group4_2D)
                {
                    if (options.BlackIsOne)
                    {
                        rowResult = DecodeRows<Group4BlackIsOneConfiguration>(ref bitReader,
                            ref decodedBitmap,
                            in options,
                            ref allocatedBufferBytes);
                    }
                    else
                    {
                        rowResult = DecodeRows<Group4BlackIsZeroConfiguration>(ref bitReader,
                            ref decodedBitmap,
                            in options,
                            ref allocatedBufferBytes);
                    }
                }
                else if (options.BlackIsOne)
                {
                    rowResult = DecodeRows<GeneralBlackIsOneConfiguration>(ref bitReader,
                        ref decodedBitmap,
                        in options,
                        ref allocatedBufferBytes);
                }
                else
                {
                    rowResult = DecodeRows<GeneralBlackIsZeroConfiguration>(ref bitReader,
                        ref decodedBitmap,
                        in options,
                        ref allocatedBufferBytes);
                }

                result = new CcittFaxDecodeResult(rowResult.Rows,
                    bitReader.BytesConsumed,
                    rowResult.Status,
                    allocatedBufferBytes);
                return true;
            }
            catch (InvalidDataException)
            {
                result = new CcittFaxDecodeResult(0, 0, CcittFaxDecodeStatus.NotCompleted, allocatedBufferBytes);
                decodedBitmap.AsSpan().Clear();
                return false;
            }
        }

        /// <summary>Paints the black interval [startPosition, endPosition) into a white-initialized row.</summary>
        /// <remarks>Boundary masks preserve adjacent pixels and white padding. Whole interior bytes use Span.Fill.</remarks>
        /// <example>For an eight-pixel row, painting [3, 5) produces 00011000 with BlackIs1,
        /// or 11100111 without it. The interval end is exclusive.</example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PaintBlackInterval<T>(Span<byte> rowPixels, int startPosition, int endPosition)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            if (endPosition <= startPosition)
            {
                return;
            }

            int firstByteIndex = startPosition >> 3;
            int lastByteIndex = (endPosition - 1) >> 3;
            int firstByteMask = 255 >> (startPosition & 7);
            int lastByteMask = 255 << (7 - ((endPosition - 1) & 7));
            if (firstByteIndex == lastByteIndex)
            {
                byte pixelMask = (byte)(firstByteMask & lastByteMask);
#if NET8_0_OR_GREATER
                rowPixels[firstByteIndex] =
 T.BlackIsOne ? (byte)(rowPixels[firstByteIndex] | pixelMask) : (byte)(rowPixels[firstByteIndex] & ~pixelMask);
#else
                rowPixels[firstByteIndex] = default(T).BlackIsOne
                    ? (byte)(rowPixels[firstByteIndex] | pixelMask)
                    : (byte)(rowPixels[firstByteIndex] & ~pixelMask);
#endif
                return;
            }

#if NET8_0_OR_GREATER
            rowPixels[firstByteIndex] =
 T.BlackIsOne ? (byte)(rowPixels[firstByteIndex] | firstByteMask) : (byte)(rowPixels[firstByteIndex] & ~firstByteMask);
#else
            rowPixels[firstByteIndex] = default(T).BlackIsOne
                ? (byte)(rowPixels[firstByteIndex] | firstByteMask)
                : (byte)(rowPixels[firstByteIndex] & ~firstByteMask);
#endif
#if NET8_0_OR_GREATER
            rowPixels.Slice(firstByteIndex + 1, lastByteIndex - firstByteIndex - 1).Fill(T.BlackIsOne ? (byte)255 : (byte)0);
#else
            rowPixels.Slice(firstByteIndex + 1, lastByteIndex - firstByteIndex - 1)
                .Fill(default(T).BlackIsOne ? (byte)255 : (byte)0);
#endif
#if NET8_0_OR_GREATER
            rowPixels[lastByteIndex] =
 T.BlackIsOne ? (byte)(rowPixels[lastByteIndex] | lastByteMask) : (byte)(rowPixels[lastByteIndex] & ~lastByteMask);
#else
            rowPixels[lastByteIndex] = default(T).BlackIsOne
                ? (byte)(rowPixels[lastByteIndex] | lastByteMask)
                : (byte)(rowPixels[lastByteIndex] & ~lastByteMask);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool BlackIsOne<T>()
            where T : struct, ICcittFaxDecodingConfiguration
        {
#if NET8_0_OR_GREATER
            return T.BlackIsOne;
#else
            return default(T).BlackIsOne;
#endif
        }

        /// <summary>Completed encoded rows and their stopping condition; stored height may be smaller.</summary>
        private readonly struct RowDecodeResult
        {
            internal int Rows { get; }
            internal CcittFaxDecodeStatus Status { get; }

            internal RowDecodeResult(int rows, CcittFaxDecodeStatus status)
            {
                Rows = rows;
                Status = status;
            }
        }

        /// <summary>Decodes complete rows, keeps their reference transitions and validates the encoded end.</summary>
        /// <remarks>Check block endings before row framing: RTC/EOFB shares the EOL prefix.
        /// Only completed rows become the next 2D reference.</remarks>
        /// <example>
        /// Reference WWWBBWWW has transitions [3, 5, 8]. Starting in white at position 0:
        /// vertical +1 ends the white run at 4; horizontal white-3/black-2 records edges 3 and 5;
        /// pass advances to reference edge 5 while continuing white, without recording an edge.
        /// Even reference indices end white runs; odd indices end black runs. Equal positions
        /// represent zero-length runs and must remain because they still change color.
        /// </example>
        private static RowDecodeResult DecodeRows<T>(ref CcittFaxBitReader bitReader,
            ref byte[] decodedBitmap,
            in CcittFaxDecodeOptions options,
            ref long allocatedBufferBytes)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            int columns = options.Columns;
            int rowCapacity = options.InitialRowCapacity;
            var compressionType = options.CompressionType;
            bool encodedByteAlign = options.EncodedByteAlign;
            bool acceptOptionalEndOfLine = options.AcceptOptionalEndOfLine;
            bool endOfBlock = options.EndOfBlock;
            bool requireEndOfLine = options.RequireEndOfLine;
            bool verifyEndMarker = options.VerifyEndMarker;
            bool allowIncompleteTermination = options.AllowIncompleteTermination;
            int declaredRows = options.DeclaredRows;
            bool allowEndOfInput = options.AllowEndOfInput;
            long maximumAllocatedBufferBytes = options.MaximumAllocatedBufferBytes;
            int maximumStoredRows = options.MaximumStoredRows;
            int maximumDecodedRows = options.MaximumDecodedRows;
            int rowByteCount = (columns + 7) / 8;
            // Reuse both arrays; entries beyond each row's transition count are stale.
            var referenceTransitions = new ushort[columns + 2];
            var currentTransitions = new ushort[columns + 2];
            // Before the first 2D row, the reference is all white: no transitions exist,
            // so reference lookups use the right edge (columns).
            int referenceTransitionCount = 0;
            byte[]? discardedRow = null;
            int rowIndex = 0;
            for (;
                 (rowIndex < rowCapacity || maximumAllocatedBufferBytes > 0 || maximumStoredRows > 0) &&
                 (maximumDecodedRows == 0 || rowIndex < maximumDecodedRows);
                 rowIndex++)
            {
                // Only complete declared Rows can authorize an abbreviated ending, never Height or capacity.
                bool exactDeclaredRows = declaredRows > 0 && rowIndex == declaredRows;
                // Unknown Rows permit only final-byte padding. Exact positive Rows also permit
                // whole zero-fill bytes; an all-zero truncated code is indistinguishable from fill.
                bool cleanEndOfInput = rowIndex > 0 && ((allowEndOfInput && bitReader.HasOnlyBytePadding) ||
                                                        allowIncompleteTermination && exactDeclaredRows &&
                                                        bitReader.TryReadZeroFillToEnd());
                // Check padding before alignment; discarded nonzero bits must not turn an invalid
                // trailer into an accepted shortened marker.
                bool allowSingleEol = allowIncompleteTermination && exactDeclaredRows &&
                                      (!encodedByteAlign || bitReader.HasZeroAlignmentPadding);
                if (encodedByteAlign)
                {
                    bitReader.AlignToByteBoundary();
                }

                if (endOfBlock)
                {
                    var endStatus = bitReader.ReadEndOfBlockStatus(compressionType,
                        allowIncompleteRtc: allowIncompleteTermination && rowIndex > 0 &&
                                            (declaredRows == 0 || exactDeclaredRows),
                        allowIncompleteEofb: allowSingleEol,
                        allowSingleGroup3Eol: allowSingleEol);
                    if (endStatus != CcittFaxDecodeStatus.NotCompleted)
                    {
                        decodedBitmap.AsSpan(Math.Min(rowIndex, rowCapacity) * rowByteCount)
                            .Fill(BlackIsOne<T>() ? (byte)0 : (byte)255);
                        bitReader.AlignToByteBoundary();
                        return new RowDecodeResult(rowIndex, endStatus);
                    }
                }

                if (cleanEndOfInput)
                {
                    bitReader.AlignToByteBoundary();
                    return new RowDecodeResult(rowIndex, CcittFaxDecodeStatus.EndOfInput);
                }

                // Rows beyond Height still update the 2D reference. Charge their logical bytes
                // to the work budget even though one scratch row stores them all.
                bool discardRow = maximumStoredRows > 0 && rowIndex >= maximumStoredRows;
                if (discardRow && (long)(rowIndex + 1) * rowByteCount > maximumAllocatedBufferBytes)
                {
                    throw new CorruptCompressedDataException("CCITT decoded rows exceed the work budget.");
                }

                if (discardRow && discardedRow is null)
                {
                    if (allocatedBufferBytes + rowByteCount > maximumAllocatedBufferBytes)
                    {
                        throw new CorruptCompressedDataException(
                            "CCITT bitmap decode buffers exceed the allocation budget.");
                    }

                    discardedRow = new byte[rowByteCount];
                    allocatedBufferBytes += rowByteCount;
                }

                if (rowIndex == rowCapacity && !discardRow)
                {
                    // Account for all replaced arrays, not just the current live bitmap. Clamp
                    // geometric growth to the remaining budget in complete rows before allocating.
                    long affordableRows = (maximumAllocatedBufferBytes - allocatedBufferBytes) / rowByteCount;
                    long newRowCount = Math.Min(Math.Max((long)rowCapacity + 1, 2L * rowCapacity), affordableRows);
                    if (newRowCount <= rowCapacity)
                    {
                        throw new CorruptCompressedDataException(
                            "CCITT bitmap decode buffers exceed the allocation budget.");
                    }

                    int newByteCount = checked((int)(newRowCount * rowByteCount));
                    Array.Resize(ref decodedBitmap, newByteCount);
                    allocatedBufferBytes += newByteCount;
                    rowCapacity = (int)newRowCount;
                }

                var rowPixels = discardRow
                    ? discardedRow.AsSpan()
                    : decodedBitmap.AsSpan(rowIndex * rowByteCount, rowByteCount);
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
                    if (acceptOptionalEndOfLine)
                    {
                        bool hasEol = bitReader.TryReadEndOfLine();
                        if (requireEndOfLine && !hasEol)
                        {
                            throw new InvalidDataException("Required CCITT EOL is missing.");
                        }
                    }

                    isOneDimensional = false;
                }
                else
                {
                    isOneDimensional =
                        bitReader.ReadRowIsOneDimensional<T>(compressionType,
                            acceptOptionalEndOfLine,
                            requireEndOfLine);
                }

                // Reference transitions stay unchanged until this row is complete. Horizontal
                // steps change color twice, vertical/1D steps once, and pass steps never.
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
                    {
                        throw new InvalidDataException("CCITT row does not progress.");
                    }

                    int operation;
                    if (isOneDimensional)
                    {
                        operation = 0;
                    }
                    else if (hasNextModeEntry)
                    {
                        hasNextModeEntry = false;
                        if (nextModeEntry == 0)
                        {
                            operation = DecodeModeBitByBit<T>(ref bitReader);
                        }
                        else
                        {
                            bitReader.ConsumeBits(nextModeEntry & ModeCodeLengthMask);
                            operation = nextModeEntry >> ModeCodeLengthBitCount;
                        }
                    }
                    else
                    {
                        operation = DecodeMode<T>(ref bitReader);
                    }

                    // 1D and horizontal mode decode run lengths; vertical/pass mode uses the reference row.
                    if (isOneDimensional || operation == HorizontalMode)
                    {
                        uint packedRunPair =
                            (isWhiteRun ? RunPairLookup.WhiteFirstPairs : RunPairLookup.BlackFirstPairs)[
                                bitReader.PeekBits<T>(RunPairLookaheadBitCount)];
                        int firstRunLength = (int)(packedRunPair & RunPairLengthMask);
                        int secondRunLength = (int)((packedRunPair >> RunPairLengthBitCount) & RunPairLengthMask);
                        // In 1D, a first run that completes the row must not consume a second run
                        // from the following row. A horizontal 2D operation always contains two runs.
                        if (packedRunPair != 0 && firstRunLength + secondRunLength <= columns - pixelPosition &&
                            (!isOneDimensional || firstRunLength < columns - pixelPosition))
                        {
                            if (currentTransitions.Length - transitionCount < 2)
                            {
                                throw new InvalidDataException("Too many CCITT transitions.");
                            }

                            bitReader.ConsumeBits((int)(packedRunPair >> RunPairCodeLengthShift));
                            int firstRunEnd = pixelPosition + firstRunLength;
                            int secondRunEnd = firstRunEnd + secondRunLength;
                            if (!isOneDimensional)
                            {
                                // Peek the next operation before painting to overlap independent work.
                                // Its bits are consumed only at the next loop iteration.
                                nextModeEntry = ModeLookup[bitReader.PeekBits<T>(ModeLookaheadBitCount)];
                                hasNextModeEntry = true;
                            }

                            PaintBlackInterval<T>(rowPixels,
                                isWhiteRun ? firstRunEnd : pixelPosition,
                                isWhiteRun ? secondRunEnd : firstRunEnd);
                            currentTransitions[transitionCount++] = checked((ushort)firstRunEnd);
                            currentTransitions[transitionCount++] = checked((ushort)secondRunEnd);
                            pixelPosition = secondRunEnd;
                            continue;
                        }

                        if (!isOneDimensional)
                        {
                            int firstRunEnd = pixelPosition +
                                              DecodeRunLength<T>(ref bitReader, isWhiteRun, columns - pixelPosition);
                            if (transitionCount == currentTransitions.Length)
                            {
                                throw new InvalidDataException("Too many CCITT transitions.");
                            }

                            currentTransitions[transitionCount++] = checked((ushort)firstRunEnd);
                            int secondRunEnd = firstRunEnd +
                                               DecodeRunLength<T>(ref bitReader, !isWhiteRun, columns - firstRunEnd);
                            nextModeEntry = ModeLookup[bitReader.PeekBits<T>(ModeLookaheadBitCount)];
                            hasNextModeEntry = true;
                            PaintBlackInterval<T>(rowPixels,
                                isWhiteRun ? firstRunEnd : pixelPosition,
                                isWhiteRun ? secondRunEnd : firstRunEnd);
                            if (transitionCount == currentTransitions.Length)
                            {
                                throw new InvalidDataException("Too many CCITT transitions.");
                            }

                            currentTransitions[transitionCount++] = checked((ushort)secondRunEnd);
                            pixelPosition = secondRunEnd;
                            continue;
                        }

                        int nextPosition = pixelPosition +
                                           DecodeRunLength<T>(ref bitReader, isWhiteRun, columns - pixelPosition);
                        if (!isWhiteRun)
                        {
                            PaintBlackInterval<T>(rowPixels, pixelPosition, nextPosition);
                        }

                        if (transitionCount == currentTransitions.Length)
                        {
                            throw new InvalidDataException("Too many CCITT transitions.");
                        }

                        currentTransitions[transitionCount++] = checked((ushort)nextPosition);
                        pixelPosition = nextPosition;
                        isWhiteRun = !isWhiteRun;
                    }
                    else
                    {
                        nextModeEntry = ModeLookup[bitReader.PeekBits<T>(ModeLookaheadBitCount)];
                        hasNextModeEntry = true;
                        // Even and odd cursors track the next reference transition for each color.
                        // They only advance because pixelPosition never moves backwards. At the row start,
                        // retain an initial zero-position transition; it changes the color.
                        referenceTransitionIndex = isWhiteRun ? whiteReferenceIndex : blackReferenceIndex;
                        while (referenceTransitionIndex < referenceTransitionCount && pixelPosition != 0 &&
                               referenceTransitions[referenceTransitionIndex] <= pixelPosition)
                        {
                            referenceTransitionIndex += 2;
                        }

                        if (isWhiteRun)
                        {
                            whiteReferenceIndex = referenceTransitionIndex;
                        }
                        else
                        {
                            blackReferenceIndex = referenceTransitionIndex;
                        }

                        // Legacy wraps this malformed pass to the first reference edge.
                        // Reject here so the filter retries with that historical recovery rule.
                        if (operation == PassMode && pixelPosition != 0 && referenceTransitionCount != 0 &&
                            referenceTransitionIndex >= referenceTransitionCount)
                        {
                            throw new InvalidDataException("CCITT pass requires the legacy decoder.");
                        }

                        // The first reference transition is b1 in the T.4/T.6 notation.
                        int referencePosition = referenceTransitionIndex < referenceTransitionCount
                            ? referenceTransitions[referenceTransitionIndex]
                            : columns;
                        int nextPosition = operation == PassMode
                            ? (referenceTransitionIndex + 1 < referenceTransitionCount
                                ? referenceTransitions[referenceTransitionIndex + 1]
                                : columns)
                            : referencePosition + operation;
                        if (nextPosition < pixelPosition || nextPosition > columns)
                        {
                            throw new InvalidDataException("Invalid CCITT vertical/pass position.");
                        }

                        if (!isWhiteRun)
                        {
                            PaintBlackInterval<T>(rowPixels, pixelPosition, nextPosition);
                        }

                        pixelPosition = nextPosition;
                        if (operation != PassMode)
                        {
                            if (transitionCount == currentTransitions.Length)
                            {
                                throw new InvalidDataException("Too many CCITT transitions.");
                            }

                            currentTransitions[transitionCount++] = checked((ushort)pixelPosition);
                            isWhiteRun = !isWhiteRun;
                        }
                    }
                }

                (referenceTransitions, currentTransitions) = (currentTransitions, referenceTransitions);
                referenceTransitionCount = transitionCount;
            }

            var status = CcittFaxDecodeStatus.RowLimit;
            if (verifyEndMarker)
            {
                // Fixed-capacity decoding needs the same boundary checks; a full bitmap is not an end marker.
                bool exactDeclaredRows = declaredRows > 0 && rowCapacity == declaredRows;
                bool cleanEndOfInput = (allowEndOfInput && bitReader.HasOnlyBytePadding) ||
                                       allowIncompleteTermination && exactDeclaredRows &&
                                       bitReader.TryReadZeroFillToEnd();
                // Check padding before alignment; discarded nonzero bits must not turn an invalid
                // trailer into an accepted shortened marker.
                bool allowSingleEol = allowIncompleteTermination && exactDeclaredRows &&
                                      (!encodedByteAlign || bitReader.HasZeroAlignmentPadding);
                if (encodedByteAlign)
                {
                    bitReader.AlignToByteBoundary();
                }

                status = bitReader.ReadEndOfBlockStatus(compressionType,
                    allowIncompleteRtc: allowIncompleteTermination && rowCapacity > 0 &&
                                        (declaredRows == 0 || exactDeclaredRows),
                    allowIncompleteEofb: allowSingleEol,
                    allowSingleGroup3Eol: allowSingleEol);
                if (status == CcittFaxDecodeStatus.NotCompleted && cleanEndOfInput)
                {
                    status = CcittFaxDecodeStatus.EndOfInput;
                }

                if (status == CcittFaxDecodeStatus.NotCompleted)
                {
                    throw new InvalidDataException("CCITT end marker not found at the bitmap capacity hint.");
                }
            }

            bitReader.AlignToByteBoundary();
            return new RowDecodeResult(rowIndex, status);
        }
    }
}