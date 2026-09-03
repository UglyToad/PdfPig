namespace UglyToad.PdfPig.Filters
{
    using System.Buffers;
    using System;
    using System.IO;
    using System.IO.Compression;
    using Tokens;
    using Core;
    using Util;

    /// <summary>
    /// The Flate filter is based on the public-domain zlib/deflate compression method, a variable-length Lempel-Ziv 
    /// adaptive compression method cascaded with adaptive Huffman coding. 
    /// It is fully defined in Internet RFCs 1950, ZLIB Compressed Data Format Specification, and
    /// 1951, DEFLATE Compressed Data Format Specification
    /// </summary>
    /// <remarks>
    /// See section 3.3.3 of the spec (version 1.7) for details on the FlateDecode filter.
    /// The flate decode filter may have a predictor function to further compress the stream.
    /// </remarks>
    public sealed class FlateFilter : IFilter
    {
        // Defaults are from table 3.7 in the spec (version 1.7)
        private const int DefaultColors = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColumns = 1;

        /// <summary>How much is inflated per read; a damaged stream costs at most one block.</summary>
        private const int BlockLength = 8192;

        /// <summary>
        /// Where the inflated data starts out, as a multiple of the compressed length. Content streams
        /// inflate to three to five times their size; the buffer doubles when that is not enough.
        /// </summary>
        private const int InitialCapacityFactor = 4;

        private const int MinimumCapacity = 4096;

        /// <summary>The largest array the runtime will hand out.</summary>
        private const int MaximumCapacity = 0x7FFFFFC7;

        private const byte Deflate32KbWindow = 120;
        private const byte ChecksumBits = 1;

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var predictor = parameters.GetIntOrDefault(NameToken.Predictor, -1);

            try
            {
                var colors = Math.Min(parameters.GetIntOrDefault(NameToken.Colors, DefaultColors), 32);
                var bitsPerComponent = parameters.GetIntOrDefault(NameToken.BitsPerComponent, DefaultBitsPerComponent);
                var columns = parameters.GetIntOrDefault(NameToken.Columns, DefaultColumns);

                return Decompress(input, predictor, colors, bitsPerComponent, columns, streamDictionary);
            }
            catch
            {
                // ignored.
            }

            return input;
        }

        private static Memory<byte> Decompress(Memory<byte> input,
            int predictor,
            int colors,
            int bitsPerComponent,
            int columns,
            DictionaryToken streamDictionary)
        {
            using var memoryStream = MemoryHelper.AsReadOnlyMemoryStream(input);
            // The first 2 bytes are the header which DeflateStream does not support.
            memoryStream.ReadByte();
            memoryStream.ReadByte();

            if (predictor > 1 && TryGetImageHeight(streamDictionary, out var height))
            {
                return DecompressImage(memoryStream, predictor, colors, bitsPerComponent, columns, height);
            }

            // The inflater writes straight into this buffer, and the predictor is undone in the
            // same buffer as the rows arrive; the caller gets a copy of just the finished length.
            var buffer = ArrayPool<byte>.Shared.Rent(
                (int)Math.Min(MaximumCapacity, Math.Max(MinimumCapacity, (long)input.Length * InitialCapacityFactor)));

            var length = 0;

            var hasPredictor = predictor > 1;
            // Rows are decoded where the inflater put them and only gathered, without their filter
            // type bytes, by the copy into the result below, so no pass is spent moving them up.
            var decoder = hasPredictor
                ? new PngPredictor.Decoder(predictor, colors, bitsPerComponent, columns, compact: false)
                : default;

            try
            {
                using (var deflate = new DeflateStream(memoryStream, CompressionMode.Decompress))
                {
                    // Read a block at a time rather than with CopyTo, because a damaged stream
                    // throws out of the read and CopyTo would discard the whole buffer it was
                    // filling. Keeping each block that did inflate is what PDFBox's
                    // FlateFilterDecoderStream does, and it is worth a lot on a large stream.
                    while (true)
                    {
                        if (buffer.Length - length < BlockLength)
                        {
                            Grow(ref buffer, length, length + BlockLength);
                        }

                        int read;

                        try
                        {
                            read = deflate.Read(buffer, length, BlockLength);
                        }
                        catch (InvalidDataException)
                        {
                            // Damaged from here on; what came before still stands.
                            break;
                        }

                        if (read == 0)
                        {
                            break;
                        }

                        length += read;

                        if (hasPredictor)
                        {
                            // The rows this block completed are decoded while the block is still
                            // in the cache; a pass over the whole stream afterwards would be cold.
                            decoder.Advance(buffer, length);
                        }
                    }
                }

                if (hasPredictor)
                {
                    var required = decoder.RequiredCapacity(length);

                    if (required > buffer.Length)
                    {
                        Grow(ref buffer, length, required);
                    }

                    length = decoder.Finish(buffer, length);
                }

#if NET
                // Every byte is overwritten by the copy below, so the runtime need not clear the array.
                var decoded = GC.AllocateUninitializedArray<byte>(length);
#else
                var decoded = new byte[length];
#endif

                if (hasPredictor)
                {
                    decoder.CopyTo(buffer, decoded);
                }
                else
                {
                    buffer.AsSpan(0, length).CopyTo(decoded);
                }

                return decoded;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// An image stream says how many rows it has, so the decoded size is known up front. The
        /// result is allocated at that size and the rows are decoded straight into it as the inflater
        /// produces them, from a small input buffer that stays in the cache; nothing is moved or copied
        /// afterwards. A /Height that turns out wrong is grown around or trimmed at the end.
        /// </summary>
        private static Memory<byte> DecompressImage(Stream compressed, int predictor, int colors, int bitsPerComponent, int columns, int height)
        {
            var decoder = new PngPredictor.Decoder(predictor, colors, bitsPerComponent, columns);
            var rowLength = decoder.RowLength;
            var stride = decoder.Stride;

            var output = AllocateResult((int)Math.Min(MaximumCapacity, (long)height * rowLength));

            // One block of inflated data plus the row it may complete.
            var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(MinimumCapacity, stride + BlockLength));
            var length = 0;

            try
            {
                using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress))
                {
                    while (true)
                    {
                        if (buffer.Length - length < BlockLength)
                        {
                            Grow(ref buffer, length, length + BlockLength);
                        }

                        int read;

                        try
                        {
                            read = deflate.Read(buffer, length, BlockLength);
                        }
                        catch (InvalidDataException)
                        {
                            // Damaged from here on; what came before still stands.
                            break;
                        }

                        if (read == 0)
                        {
                            break;
                        }

                        length += read;

                        var completeRows = (length - decoder.ConsumedLength) / stride;
                        EnsureOutput(ref output, decoder.DecodedLength + (completeRows * rowLength));

                        decoder.Advance(buffer, length, output);

                        // Only the input that has not made a row yet is kept, at the start of the buffer.
                        var tail = length - decoder.ConsumedLength;
                        buffer.AsSpan(decoder.ConsumedLength, tail).CopyTo(buffer);
                        length = tail;
                        decoder.RestartInput();
                    }
                }

                EnsureOutput(ref output, decoder.FinalLength(length));

                var decodedLength = decoder.Finish(buffer, length, output);

                if (decodedLength == output.Length)
                {
                    return output;
                }

                // The stream had fewer rows than /Height promised.
                var exact = AllocateResult(decodedLength);
                output.AsSpan(0, decodedLength).CopyTo(exact);

                return exact;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static bool TryGetImageHeight(DictionaryToken streamDictionary, out int height)
        {
            height = 0;

            if (streamDictionary.TryGet(NameToken.Height, out var token) && token is NumericToken number && number.Int > 0)
            {
                height = number.Int;
                return true;
            }

            return false;
        }

        private static byte[] AllocateResult(int length)
        {
#if NET
            // Every byte is written before the array is handed out, so the runtime need not clear it.
            return GC.AllocateUninitializedArray<byte>(length);
#else
            return new byte[length];
#endif
        }

        /// <summary>Grows the result when a stream has more rows than its /Height said.</summary>
        private static void EnsureOutput(ref byte[] output, int required)
        {
            if (required > output.Length)
            {
                var grown = AllocateResult((int)Math.Min(MaximumCapacity, Math.Max(required, output.Length * 2L)));
                output.AsSpan().CopyTo(grown);
                output = grown;
            }
        }

        private static void Grow(ref byte[] buffer, int length, int required)
        {
            var grown = ArrayPool<byte>.Shared.Rent((int)Math.Min(MaximumCapacity, Math.Max(required, buffer.Length * 2L)));

            buffer.AsSpan(0, length).CopyTo(grown);
            ArrayPool<byte>.Shared.Return(buffer);

            buffer = grown;
        }

        /// <summary>
        /// Convert a decoded data stream back to the encoded version.
        /// </summary>
        /// <param name="input">The decoded data.</param>
        /// <param name="streamDictionary">The stream dictionary with the parameters to use.</param>
        /// <returns>The Flate encoded data.</returns>
        public byte[] Encode(Stream input, DictionaryToken streamDictionary)
        {
            const int headerLength = 2;
            const int checksumLength = 4;

            byte[] data;
            using (var temp = new MemoryStream())
            {
                input.CopyTo(temp);
                data = temp.ToArray();
            }

            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionLevel.Fastest))
            using (var adlerStream = new Adler32ChecksumStream(compressor))
            {
                adlerStream.Write(data, 0, data.Length);
                adlerStream.Close();

                var compressed = compressStream.ToArray();

                var result = new byte[headerLength + compressed.Length + checksumLength];

                // Write the ZLib header.
                result[0] = Deflate32KbWindow;
                result[1] = ChecksumBits;

                // Write the compressed data.
                Array.Copy(compressed, 0, result, headerLength, compressed.Length);

                // Write Checksum of raw data.
                var checksum = adlerStream.Checksum;

                var offset = headerLength + compressed.Length;

                result[offset++] = (byte)(checksum >> 24);
                result[offset++] = (byte)(checksum >> 16);
                result[offset++] = (byte)(checksum >> 8);
                result[offset] = (byte)(checksum >> 0);

                return result;
            }
        }
    }
}