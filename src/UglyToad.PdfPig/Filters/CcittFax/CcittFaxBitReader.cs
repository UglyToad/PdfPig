namespace UglyToad.PdfPig.Filters.CcittFax
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>Reads most-significant-bit-first CCITT codes from a span using a 64-bit reservoir.</summary>
    /// <remarks>
    /// <para>A ref struct keeps the input span without copying or allocating a stream wrapper.
    /// The reservoir batches byte loads so run and mode decoding do not fetch each bit separately.
    /// Store unread bits in the low buffered-bit-count positions, with the next bit
    /// at their upper end. Append four bytes in big-endian order when at most 32 bits remain;
    /// refill short input tails byte by byte. Peek extracts a prefix without consuming its code.</para>
    /// <para>When a complete prefix is already buffered, extract it before loading the next word.
    /// This shortens the dependent work before a lookup result is available. Compact lookups may index using
    /// zero-padded missing bits, but consumption checks the actual buffered count. Exact reads instead
    /// report exhausted input during EOL framing probes.</para>
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
    internal ref struct CcittFaxBitReader
    {
        // The low bufferedBitCount bits are unread; bit bufferedBitCount - 1 is consumed next.
        // Higher bits are ignored.
        // inputOffset is the index of the next byte to append to bitBuffer.
        private readonly ReadOnlySpan<byte> compressedInput;
        private int inputOffset;
        private int bufferedBitCount;
        private ulong bitBuffer;

        internal CcittFaxBitReader(ReadOnlySpan<byte> compressedInput)
        {
            this.compressedInput = compressedInput;
            inputOffset = 0;
            bufferedBitCount = 0;
            bitBuffer = 0;
        }

        /// <summary>Bytes containing consumed bits, including the final byte's unused bits.</summary>
        /// <remarks>Subtract whole unread bytes so reservoir lookahead is not counted as consumption.
        /// This is ceil((8 * inputOffset - bufferedBitCount) / 8), without multiplying an Int32 offset.</remarks>
        /// <example>If two bytes were loaded and only three bits consumed, 13 bits remain buffered:
        /// 2 - (13 / 8) = 1 byte consumed. Loading ahead does not move the logical boundary.
        /// After nine bits are consumed, seven remain and the boundary becomes two bytes.</example>
        internal int BytesConsumed => inputOffset - bufferedBitCount / 8;

        /// <summary>Peeks a code prefix, allowing zero padding only for lookup indexing.</summary>
        /// <remarks>The actual buffered count is unchanged by padding. ConsumeBits must check
        /// the selected code's true length before it is accepted.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int PeekBits<T>(int bitCount)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            // Compute the prefix before loading and appending the next word. The guards ensure
            // that the prefix is complete and the 32-bit append cannot overflow the reservoir.
            if (bufferedBitCount >= bitCount && bufferedBitCount <= 32 && compressedInput.Length - inputOffset >= 4)
            {
                int prefixValue = (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1));
                ulong refilledBuffer = (bitBuffer << 32) |
                                       BinaryPrimitives.ReadUInt32BigEndian(compressedInput.Slice(inputOffset, 4));
                inputOffset += 4;
                bitBuffer = refilledBuffer;
                bufferedBitCount += 32;
                return prefixValue;
            }

            if (bufferedBitCount < bitCount)
            {
                Refill(bitCount);
            }

            return bufferedBitCount >= bitCount
                ? (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1))
                : (int)((bitBuffer << (bitCount - bufferedBitCount)) & ((1UL << bitCount) - 1));
        }

        /// <summary>Loads a four-byte word when space permits, then individual bytes for the input tail.</summary>
        /// <remarks>Never read beyond the span. Reaching EOF may leave fewer bits than requested;
        /// the caller decides whether that is acceptable lookahead or a truncated code.</remarks>
        private void Refill(int bitCount)
        {
            if (compressedInput.Length - inputOffset >= 4 && bufferedBitCount <= 32)
            {
                bitBuffer = (bitBuffer << 32) |
                            BinaryPrimitives.ReadUInt32BigEndian(compressedInput.Slice(inputOffset, 4));
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
            {
                throw new InvalidDataException("Truncated CCITT code.");
            }

            bufferedBitCount -= bitCount;
        }

        /// <summary>Reads and consumes a code prefix, rejecting any bits supplied only by lookup padding.</summary>
        internal int ReadBits<T>(int bitCount)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            var prefixValue = PeekBits<T>(bitCount);
            ConsumeBits(bitCount);
            return prefixValue;
        }

        /// <summary>Checks unread bits up to the next byte boundary before alignment discards them.</summary>
        /// <remarks>This checks only the current partial byte, not the rest of the input. Whole
        /// bytes may still contain an EOL. Because refills append whole bytes, bufferedBitCount
        /// modulo eight equals the number of unread bits to the next input-byte boundary.</remarks>
        internal bool HasZeroAlignmentPadding
        {
            get
            {
                int paddingBits = bufferedBitCount & 7;
                return paddingBits == 0 || PeekBufferedBits(paddingBits) == 0;
            }
        }

        /// <summary>True when the entire remaining input is at most seven zero bits.</summary>
        /// <remarks>Include unread reservoir bits and bytes not loaded into the reservoir. Eight
        /// or more remaining bits cannot be final-byte padding, even if all are zero. This is an
        /// EOF check, unlike HasZeroAlignmentPadding, which permits subsequent whole bytes.</remarks>
        internal bool HasOnlyBytePadding
        {
            get
            {
                long remaining = bufferedBitCount + 8L * (compressedInput.Length - inputOffset);
                if (remaining > 7)
                {
                    return false;
                }

                return remaining == 0 || (EnsureBits((int)remaining) && PeekBufferedBits((int)remaining) == 0);
            }
        }

        /// <summary>Discards unread bits of the current partial byte without checking their value.</summary>
        /// <remarks>Callers accepting an EOF or shortened-EOFB exception must check the relevant
        /// zero-padding condition first. Alignment alone is not proof of valid termination.</remarks>
        internal void AlignToByteBoundary() => bufferedBitCount -= bufferedBitCount & 7;

        /// <summary>Consumes row framing and returns whether this row uses 1D run coding.</summary>
        /// <remarks>MH and Group 3 1D always return true; Group 4 returns false. Mixed Group 3
        /// reads a tag: one selects 1D and zero selects 2D. MH probes only optional EOL; Group 3
        /// checks requireEndOfLine. Group 4 checks it when optional framing probes are enabled.
        /// Failed optional probes leave ordinary row data unconsumed.</remarks>
        internal bool ReadRowIsOneDimensional<T>(CcittFaxCompressionType compressionType,
            bool acceptOptionalEndOfLine,
            bool requireEndOfLine)
            where T : struct, ICcittFaxDecodingConfiguration
        {
            if (compressionType == CcittFaxCompressionType.ModifiedHuffman)
            {
                if (acceptOptionalEndOfLine)
                {
                    TryReadEndOfLine();
                }

                return true;
            }

            if (compressionType == CcittFaxCompressionType.Group4_2D)
            {
                if (acceptOptionalEndOfLine)
                {
                    bool hasEol = TryReadEndOfLine();
                    if (requireEndOfLine && !hasEol)
                    {
                        throw new InvalidDataException("Required CCITT EOL is missing.");
                    }
                }

                return false;
            }

            if (!requireEndOfLine)
            {
                TryReadEndOfLine();
                return compressionType == CcittFaxCompressionType.Group3_1D || ReadBits<T>(1) != 0;
            }

            if (!TryReadEndOfLine())
            {
                throw new InvalidDataException("Required CCITT EOL is missing.");
            }

            return compressionType == CcittFaxCompressionType.Group3_1D || ReadBits<T>(1) != 0;
        }

        /// <summary>Consumes an EOL, including preceding zero fill, only when the complete marker exists.</summary>
        /// <remarks>Read eleven or more zero bits followed by one. Additional leading zeros are
        /// fill, not additional EOLs: each EOL must end in its own set bit. A copy of the reader
        /// makes a failed probe leave the original logical bit position intact. This decoder uses
        /// EOL for row framing and for the RTC/EOFB probes below.</remarks>
        internal bool TryReadEndOfLine()
        {
            // Ordinary Huffman/mode prefixes cannot begin with eleven zero bits. Reject them
            // with one reservoir lookup; only possible EOL/fill prefixes need a bitwise probe.
            if (EnsureBits(12) && PeekBufferedBits(12) > 1)
            {
                return false;
            }

            var probe = this;
            int zeroCount = 0;
            while (probe.EnsureBits(1))
            {
                if (probe.ReadBitsExact(1) != 0)
                {
                    if (zeroCount < 11)
                    {
                        return false;
                    }

                    this = probe;
                    return true;
                }

                if (zeroCount < 11)
                {
                    zeroCount++;
                }
            }

            return false;
        }

        /// <summary>Consumes a complete RTC/EOFB or a caller-authorized, zero-terminated short marker.</summary>
        /// <remarks>Probe a copy so an ordinary row EOL remains available when it is not a block end.
        /// RTC (return-to-control) is six EOLs for Group 3; EOFB (end-of-facsimile-block) is two
        /// EOLs for Group 4. Every mixed Group 3 RTC tag must select 1D. A tag does not encode a pixel.
        /// The caller checks completed rows before
        /// enabling any short-marker tolerance; this reader checks only the actual trailer bits.</remarks>
        internal CcittFaxDecodeStatus ReadEndOfBlockStatus(CcittFaxCompressionType compressionType,
            bool allowIncompleteRtc = false,
            bool allowIncompleteEofb = false,
            bool allowSingleGroup3Eol = false)
        {
            if (EnsureBits(12) && PeekBufferedBits(12) > 1)
            {
                return CcittFaxDecodeStatus.NotCompleted;
            }

            return ReadEndOfBlockStatusCore(compressionType,
                allowIncompleteRtc,
                allowIncompleteEofb,
                allowSingleGroup3Eol);
        }

        private CcittFaxDecodeStatus ReadEndOfBlockStatusCore(CcittFaxCompressionType compressionType,
            bool allowIncompleteRtc,
            bool allowIncompleteEofb,
            bool allowSingleGroup3Eol)
        {
            var probe = this;
            int eolCount = compressionType == CcittFaxCompressionType.Group4_2D ? 2 : 6;
            for (int i = 0; i < eolCount; i++)
            {
                if (!probe.TryReadEndOfLine())
                {
                    // A single Group 4 EOL is tolerated only when the caller verified the exact
                    // declared row count. All remaining bits, including entire bytes, must be zero.
                    if (allowIncompleteEofb && compressionType == CcittFaxCompressionType.Group4_2D && i == 1 &&
                        probe.TryReadZeroFillToEnd())
                    {
                        probe.AlignToByteBoundary();
                        this = probe;
                        return CcittFaxDecodeStatus.IncompleteEndOfBlock;
                    }

                    // One Group 3 EOL is a separate local EOF tolerance, not a complete RTC.
                    // The caller requires exact positive Rows without repairs. As with shortened RTCs,
                    // every remaining bit must be zero; this may include whole zero-fill bytes.
                    if (allowSingleGroup3Eol && compressionType != CcittFaxCompressionType.Group4_2D && i == 1 &&
                        probe.TryReadZeroFillToEnd())
                    {
                        probe.AlignToByteBoundary();
                        this = probe;
                        return CcittFaxDecodeStatus.IncompleteEndOfBlock;
                    }

                    // Compatibility at EOF, not a shortened RTC definition: retain complete rows
                    // when the caller permits Group 3 termination with at least two EOLs and zero fill.
                    // The caller checks the declared row count; this reader only validates the trailer.
                    // Behavior comparison only; no Poppler implementation code was copied.
                    // Poppler also retains decoded data after reporting an incomplete RTC:
                    // https://skia.googlesource.com/third_party/poppler/+/db9634f3a05c92b3687881259afdf707e1d614d1/poppler/Stream.cc#2252
                    if (!allowIncompleteRtc || compressionType == CcittFaxCompressionType.Group4_2D || i < 2 ||
                        !probe.TryReadZeroFillToEnd())
                    {
                        return CcittFaxDecodeStatus.NotCompleted;
                    }

                    this = probe;
                    return CcittFaxDecodeStatus.IncompleteEndOfBlock;
                }

                if (compressionType == CcittFaxCompressionType.Group3_2D &&
                    (!probe.EnsureBits(1) || probe.ReadBitsExact(1) != 1))
                {
                    return CcittFaxDecodeStatus.NotCompleted;
                }
            }

            this = probe;
            return CcittFaxDecodeStatus.EndOfBlock;
        }

        /// <summary>Consumes the remaining input only when every remaining bit is zero.</summary>
        /// <remarks>This scans buffered bits and input once without allocating an output buffer.
        /// Probe a copy: a nonzero bit preserves the original logical input position. Success
        /// consumes through EOF, including whole zero bytes, so BytesConsumed includes the fill.
        /// Callers must separately authorize this local termination tolerance from exact positive
        /// Rows without repairs, or the existing two-to-five-EOL Group 3 rule. Unknown Rows alone
        /// authorize only HasOnlyBytePadding, not arbitrary zero fill. All-zero tails can also be
        /// truncated code prefixes; the reader cannot distinguish those from intended fill.</remarks>
        internal bool TryReadZeroFillToEnd()
        {
            var probe = this;
            if (!probe.ReadZeroFillToEnd())
            {
                return false;
            }

            this = probe;
            return true;
        }

        private bool ReadZeroFillToEnd()
        {
            while (EnsureBits(1))
            {
                int bitCount = Math.Min(bufferedBitCount, 32);
                if (PeekBufferedBits(bitCount) != 0)
                {
                    return false;
                }

                ConsumeBits(bitCount);
            }

            return true;
        }

        /// <summary>Refills until the requested prefix is present or compressed input ends.</summary>
        /// <returns>True only when all requested bits come from real input.</returns>
        internal bool EnsureBits(int bitCount)
        {
            if (bufferedBitCount < bitCount)
            {
                Refill(bitCount);
            }

            return bufferedBitCount >= bitCount;
        }

        /// <summary>Extracts already buffered bits without refilling, padding or consuming them.</summary>
        /// <remarks>Call only after EnsureBits(bitCount) succeeds. Framing probes require real bits;
        /// padded lookup bits cannot establish an EOL or an image boundary.</remarks>
        internal int PeekBufferedBits(int bitCount) =>
            (int)((bitBuffer >> (bufferedBitCount - bitCount)) & ((1UL << bitCount) - 1));

        /// <summary>Reads and consumes a complete code fragment without zero padding.</summary>
        /// <exception cref="EndOfStreamException">Compressed input ends before the requested bits are available.</exception>
        internal int ReadBitsExact(int bitCount)
        {
            if (!EnsureBits(bitCount))
            {
                throw new EndOfStreamException("Unexpected end of Huffman RLE stream");
            }

            int prefixValue = PeekBufferedBits(bitCount);
            bufferedBitCount -= bitCount;
            return prefixValue;
        }
    }
}