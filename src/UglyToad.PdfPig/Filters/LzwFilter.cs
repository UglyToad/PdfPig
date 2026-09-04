namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using Core;
    using Lzw;
    using Tokens;
    using Util;

    /// <summary>
    /// The LZW (Lempel-Ziv-Welch) filter is a variable-length, adaptive compression method
    /// that has been adopted as one of the standard compression methods in the Tag Image File Format (TIFF) standard.
    /// </summary>
    /// <remarks>
    /// See section 7.4.4 of ISO 32000-1. The code table holds no sequences: every code above the
    /// literals names a range of the output already written, so decoding a code is a copy from
    /// earlier in the output, much as an inflater serves a back-reference from its window.
    /// </remarks>
    public sealed class LzwFilter : IFilter
    {
        private const int ClearTable = 256;
        private const int EodMarker = 257;

        /// <summary>The first code the table assigns to a sequence; below it are the literals and the two markers.</summary>
        private const int FirstFreeCode = 258;

        /// <summary>Codes are at most 12 bits wide, so the table never grows beyond this.</summary>
        private const int MaxCodes = 4096;

        private const int MinCodeBits = 9;
        private const int MaxCodeBits = 12;

        // The table sizes at which the codes get a bit wider, before the EarlyChange offset.
        private const int NineBitBoundary = 511;
        private const int TenBitBoundary = 1023;
        private const int ElevenBitBoundary = 2047;

        /// <summary>How much larger than the input the output is assumed to be when the buffer is first rented.</summary>
        private const int ExpectedExpansion = 3;

        /// <summary>
        /// The most LZW can expand by when the encoder clears the table as the specification
        /// requires: strings grow by a byte per code, so a run yields about 7.4 MB from the 5.4 KB
        /// of codes that fill a 12-bit table, roughly 1400 to one. A decoder that goes on with a
        /// full table, as this one does, would allow about 2600; the stricter figure is the bound.
        /// </summary>
        private const int MaximumExpansion = 1400;

        /// <summary>The literal bytes 0 to 255 are written ahead of the output so that a literal code is a range of the buffer like any other.</summary>
        private const int LiteralPreamble = 256;

        private const int MinimumCapacity = 1024;

        /// <summary>How many bytes past the written output are kept spare, so that <see cref="Copy"/> may move whole words.</summary>
        private const int CopySlack = 2 * sizeof(ulong);

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var (predictor, colors, bitsPerComponent, columns) = PngPredictor.Parameters.Read(parameters);

            var earlyChange = parameters.GetIntOrDefault(NameToken.EarlyChange, 1);
            return Decode(input.Span, earlyChange == 1, predictor, colors, bitsPerComponent, columns, streamDictionary);
        }

        private static Memory<byte> Decode(ReadOnlySpan<byte> input, bool isEarlyChange, int predictor, int colors, int bitsPerComponent, int columns, DictionaryToken streamDictionary)
        {
            // The output is decoded into a rented buffer and only the finished length is handed to
            // the caller, so nothing oversized stays alive behind the result. The buffer is sized
            // from the dictionary where that states the decoded length, plus the literal preamble
            // the decoder keeps in front of the data.
            var buffer = ArrayPool<byte>.Shared.Rent(
                (int)Math.Min(DecodeBuffer.MaximumCapacity, (long)DecodeBuffer.Capacity(input.Length, streamDictionary, ExpectedExpansion, MinimumCapacity, MaximumExpansion) + LiteralPreamble));

            try
            {
                var length = Decode(input, isEarlyChange, ref buffer);

                // The Flate filter decodes rows as they inflate, straight into the result, which
                // spares it a buffer for the whole inflated stream. The LZW decoder needs that
                // buffer anyway, since its table is the output it has written so far, so the
                // stream is decoded whole and the predictor is undone in the pass that moves the
                // data out of the rented buffer, as the Brotli filter does too. Most streams carry
                // no predictor at all, and then the pass is the copy.
                return PngPredictor.DecodeToArray(buffer.AsSpan(LiteralPreamble, length), predictor, colors, bitsPerComponent, columns);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Decodes the LZW codes into <paramref name="output"/> after its first <see cref="LiteralPreamble"/> bytes,
        /// growing it as needed, and returns the number of bytes decoded.
        /// </summary>
        private static int Decode(ReadOnlySpan<byte> input, bool isEarlyChange, ref byte[] output)
        {
            // The table is the output itself. Every code the decoder adds stands for the sequence it
            // has just written followed by the first byte of the next one, and those bytes sit next
            // to each other in the output. So a code needs only where its sequence starts and how
            // long it is, and decoding it is a copy out of the output - the way an inflater serves
            // a back-reference from its window rather than from a table of strings.
            //
            // The arrays are rented uncleared: an entry is only ever read after it was written.
            var offsets = ArrayPool<int>.Shared.Rent(MaxCodes);
            var lengths = ArrayPool<ushort>.Shared.Rent(MaxCodes);

            try
            {
                // The literals are laid out before the output so that they, too, are ranges of the
                // buffer and every code takes the same path.
                for (var i = 0; i < LiteralPreamble; i++)
                {
                    output[i] = (byte)i;
                    offsets[i] = i;
                    lengths[i] = 1;
                }

                var position = LiteralPreamble;

                var nextCode = FirstFreeCode;
                var codeOffset = isEarlyChange ? 0 : 1;
                var codeBits = MinCodeBits;
                var nextBoundary = NineBitBoundary + codeOffset;

                // The code before this one, with where and how long its sequence was written.
                var previous = -1;
                var previousPosition = 0;
                var previousLength = 0;

                var reader = new BitStream(input);

                // Data that runs out without an EOD marker is treated as ended; what was decoded stands.
                while (reader.TryGet(codeBits, out var code))
                {
                    if (code == EodMarker)
                    {
                        break;
                    }

                    if (code == ClearTable)
                    {
                        nextCode = FirstFreeCode;
                        codeBits = MinCodeBits;
                        nextBoundary = NineBitBoundary + codeOffset;
                        previous = -1;
                        continue;
                    }

                    int sequenceLength;

                    if (code < nextCode)
                    {
                        sequenceLength = lengths[code];

                        EnsureCapacity(ref output, position, (long)position + Math.Max(sequenceLength, CopySlack));

                        Copy(output, offsets[code], position, sequenceLength);
                    }
                    else if (previous >= 0)
                    {
                        // The code is not in the table yet: it can only be the previous sequence
                        // followed by its own first byte, the case the encoder emits when a sequence
                        // repeats itself immediately.
                        sequenceLength = previousLength + 1;

                        EnsureCapacity(ref output, position, (long)position + Math.Max(sequenceLength, CopySlack));

                        Copy(output, previousPosition, position, previousLength);
                        output[position + previousLength] = output[position];
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid LZW code {code} at output offset {position - LiteralPreamble}: the code is not in the table and no sequence precedes it.");
                    }

                    if (previous >= 0 && nextCode < MaxCodes)
                    {
                        // The previous sequence and the first byte of this one, which is the byte
                        // right after it.
                        offsets[nextCode] = previousPosition;
                        lengths[nextCode] = (ushort)(previousLength + 1);
                        nextCode++;

                        // The table grows by at most one per code, so a single comparison against
                        // the next boundary keeps the width right.
                        if (nextCode >= nextBoundary && codeBits < MaxCodeBits)
                        {
                            codeBits++;
                            nextBoundary = codeBits switch
                            {
                                10 => TenBitBoundary + codeOffset,
                                11 => ElevenBitBoundary + codeOffset,
                                _ => int.MaxValue
                            };
                        }
                    }

                    previous = code;
                    previousPosition = position;
                    previousLength = sequenceLength;
                    position += sequenceLength;
                }

                return position - LiteralPreamble;
            }
            finally
            {
                ArrayPool<int>.Shared.Return(offsets);
                ArrayPool<ushort>.Shared.Return(lengths);
            }
        }

        /// <summary>
        /// Copies a sequence from earlier in the output to <paramref name="to"/>. Most sequences are a
        /// few bytes long, and for those a whole word is moved rather than exactly the bytes asked
        /// for: the surplus lands beyond the sequence, in output not yet written, and is overwritten
        /// by whatever comes next. The buffer always has <see cref="CopySlack"/> bytes spare there.
        /// </summary>
        private static void Copy(byte[] output, int from, int to, int length)
        {
            if (length <= sizeof(ulong))
            {
                BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(to), BinaryPrimitives.ReadUInt64LittleEndian(output.AsSpan(from)));
            }
            else if (length <= 2 * sizeof(ulong))
            {
                var first = BinaryPrimitives.ReadUInt64LittleEndian(output.AsSpan(from));
                var second = BinaryPrimitives.ReadUInt64LittleEndian(output.AsSpan(from + sizeof(ulong)));

                BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(to), first);
                BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(to + sizeof(ulong)), second);
            }
            else
            {
                new ReadOnlySpan<byte>(output, from, length).CopyTo(new Span<byte>(output, to, length));
            }
        }

        /// <summary>Makes room for <paramref name="required"/> bytes, carrying over the <paramref name="written"/> that are there.</summary>
        private static void EnsureCapacity(ref byte[] output, int written, long required)
        {
            if (required > output.Length)
            {
                DecodeBuffer.Grow(ref output, written, required);
            }
        }
    }
}
