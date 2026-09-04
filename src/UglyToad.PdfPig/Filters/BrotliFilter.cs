namespace UglyToad.PdfPig.Filters
{
    using System;
    using UglyToad.PdfPig.Tokens;
#if NET || NETSTANDARD2_1_OR_GREATER
    using System.Buffers;
    using System.IO;
    using System.IO.Compression;
    using Core;
    using Fonts;
    using Util;
#endif

    /// <summary>
    /// Brotli (IETF RFC 7932) is a general-purpose, lossless compression algorithm. The Brotli filter decodes
    /// data that has been encoded using Brotli compression. It may be cascaded with a predictor in the same way
    /// as the <see cref="FlateFilter"/> and <see cref="LzwFilter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The filter is defined by "Brotli compression in PDF 2.0" (EXTN-BROTLI-1, PDF Association), an extension
    /// to ISO 32000. The extension adds BrotliDecode to Clause 7.4.1, Table 6 - Standard Filters, and extends
    /// the LZWDecode and FlateDecode predictor parameters of Clause 7.4.4.3 to cover it, which is why the
    /// predictor handling here matches <see cref="FlateFilter"/>.
    /// </para>
    /// <para>
    /// The stream carries no header of its own: unlike the zlib wrapper the Flate filter has to step over, a
    /// BrotliDecode stream is the raw RFC 7932 bitstream. The extension also forbids the framing format and
    /// shared dictionaries of IETF RFC 9841, and forbids BrotliDecode for inline images.
    /// </para>
    /// <para>
    /// A stream that is cut short or damaged is reported rather than returned in part. This is why decoding
    /// goes through <c>BrotliDecoder</c> and not <c>BrotliStream</c>: the stream class ends a truncated stream
    /// as though it had finished, so the caller receives half the content with nothing to distinguish it from
    /// all of it, while the decoder returns a status for every block and makes that difference visible.
    /// </para>
    /// <para>
    /// Large window Brotli (IETF RFC 9841), which the extension requires, is <b>not</b> supported. The decoder
    /// tops out at the RFC 7932 window of 2^24 bytes and offers no way to opt in to the larger windows; a
    /// stream that uses one is reported as corrupt. The C# decoder in the reference implementation at
    /// https://github.com/google/brotli carries the same 2^24 limit, so replacing the decoder with it would
    /// not lift this restriction - only the reference C implementation does, through
    /// BROTLI_DECODER_PARAM_LARGE_WINDOW. In practice a larger window only pays off for streams above 16MB.
    /// </para>
    /// </remarks>
    public sealed class BrotliFilter : IFilter
    {
#if NET || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Where the decompressed data starts out, as a multiple of the compressed length. Brotli
        /// reaches far higher ratios than this, but a bigger rent costs more than the doubling it
        /// saves - measured over the streams of the sample documents, which average about 9x.
        /// </summary>
        private const int InitialCapacityFactor = 2;

        private const int MinimumCapacity = 1024;

        private const string InvalidStreamMessage =
            "Invalid Brotli compressed stream encountered. A stream using large window Brotli "
            + "(IETF RFC 9841) fails in the same way, as the decoder does not support it.";

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var (predictor, colors, bitsPerComponent, columns) = PngPredictor.Parameters.Read(parameters);

            if (input.Length == 0)
            {
                // No bytes is not a Brotli stream at all, but PDFs do carry empty streams and the
                // other filters hand back nothing rather than object to them.
                return Memory<byte>.Empty;
            }

            // The decoder writes straight into this buffer, so there is no copy per block, and the
            // buffer is rented because only the finished length is handed to the caller. It is sized
            // from the dictionary where that states the decoded length, as embedded files do. A
            // single Brotli copy may run to 16 MB, so no expansion ratio bounds what a stated length
            // may plausibly be; only the absolute ceiling does.
            var buffer = ArrayPool<byte>.Shared.Rent(DecodeBuffer.Capacity(input.Length, streamDictionary, InitialCapacityFactor, MinimumCapacity, DecodeBuffer.UnboundedExpansion));

            try
            {
                var total = Decompress(input, ref buffer);

                // The Flate filter decodes rows as they inflate, straight into the result, which
                // spares it a buffer for the whole inflated stream. The Brotli decoder hands over
                // whatever fits, not rows, so the stream is decompressed whole and the predictor
                // is undone in the pass that moves the data out of the rented buffer. Measured on
                // a predicted image that pass costs what the plain copy did, so a row-by-row path
                // for Brotli would have little to gain.
                return PngPredictor.DecodeToArray(buffer.AsSpan(0, total), predictor, colors, bitsPerComponent, columns);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Decompresses the whole stream into <paramref name="buffer"/>, growing it as needed, and returns
        /// the number of bytes decompressed.
        /// </summary>
        private static int Decompress(Memory<byte> input, ref byte[] buffer)
        {
            var total = 0;

            try
            {
                using var decoder = new BrotliDecoder();

                var remaining = (ReadOnlyMemory<byte>)input;

                while (true)
                {
                    var status = decoder.Decompress(remaining.Span, buffer.AsSpan(total), out var consumed, out var written);

                    total += written;
                    remaining = remaining.Slice(consumed);

                    if (status == OperationStatus.Done)
                    {
                        // Anything left in the input after the stream ended is not ours to read.
                        return total;
                    }

                    if (status == OperationStatus.NeedMoreData)
                    {
                        // Everything there was has been consumed and the compressed stream still
                        // has not ended, so the data ran out early. Damaged data arrives here too:
                        // a bitstream that was altered no longer reaches its end marker.
                        throw new CorruptCompressedDataException(
                            "Truncated or damaged Brotli compressed stream encountered: the data "
                            + $"ran out before the compressed stream ended, after {total} bytes.");
                    }

                    if (status != OperationStatus.DestinationTooSmall || (consumed == 0 && written == 0))
                    {
                        throw new CorruptCompressedDataException(InvalidStreamMessage);
                    }

                    // The buffer doubles; a stream too large for one array is refused there.
                    DecodeBuffer.Grow(ref buffer, total, buffer.Length + 1L);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is InvalidDataException)
            {
                throw new CorruptCompressedDataException(InvalidStreamMessage, ex);
            }
        }
#else
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            throw new NotSupportedException(
                "The BrotliDecode filter is only supported on .NET Standard 2.1, .NET Core and .NET 5.0 or greater targets.");
        }
#endif
    }
}
