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
        // Defaults are from ISO 32000, Clause 7.4.4.3, Table 8, shared with the Flate/LZW predictors.
        private const int DefaultColors = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColumns = 1;

        /// <summary>
        /// Where the decompressed data starts out, as a multiple of the compressed length. Brotli
        /// reaches far higher ratios than this, but a bigger rent costs more than the doubling it
        /// saves - measured over the streams of the sample documents, which average about 9x.
        /// </summary>
        private const int InitialCapacityFactor = 2;

        private const int MinimumCapacity = 1024;

        /// <summary>The largest array the runtime will hand out.</summary>
        private const int MaximumCapacity = 0x7FFFFFC7;

        private const string InvalidStreamMessage =
            "Invalid Brotli compressed stream encountered. A stream using large window Brotli "
            + "(IETF RFC 9841) fails in the same way, as the decoder does not support it.";

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var predictor = parameters.GetIntOrDefault(NameToken.Predictor, -1);
            var colors = Math.Min(parameters.GetIntOrDefault(NameToken.Colors, DefaultColors), 32);
            var bitsPerComponent = parameters.GetIntOrDefault(NameToken.BitsPerComponent, DefaultBitsPerComponent);
            var columns = parameters.GetIntOrDefault(NameToken.Columns, DefaultColumns);

            var decoded = Decompress(input);

            // Undone in place; below 2 the data comes straight back.
            return PngPredictor.Decode(decoded, predictor, colors, bitsPerComponent, columns);
        }

        private static byte[] Decompress(Memory<byte> input)
        {
            if (input.Length == 0)
            {
                // No bytes is not a Brotli stream at all, but PDFs do carry empty streams and the
                // other filters hand back nothing rather than object to them.
                return [];
            }

            // The decoder writes straight into this buffer, so there is no copy per block, and the
            // buffer is rented because only the finished length is handed to the caller.
            var buffer = ArrayPool<byte>.Shared.Rent(
                (int)Math.Min(MaximumCapacity, Math.Max(MinimumCapacity, (long)input.Length * InitialCapacityFactor)));

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
#if NET
                        // Every byte is overwritten by the copy below, so the runtime does not
                        // need to clear the array first.
                        var decoded = GC.AllocateUninitializedArray<byte>(total);
#else
                        var decoded = new byte[total];
#endif
                        buffer.AsSpan(0, total).CopyTo(decoded);

                        return decoded;
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

                    var grownLength = (int)Math.Min(MaximumCapacity, (long)buffer.Length * 2);

                    if (grownLength == buffer.Length)
                    {
                        throw new CorruptCompressedDataException(
                            "Brotli compressed stream decodes to more than can be held in one array.");
                    }

                    var grown = ArrayPool<byte>.Shared.Rent(grownLength);

                    buffer.AsSpan(0, total).CopyTo(grown);
                    ArrayPool<byte>.Shared.Return(buffer);

                    buffer = grown;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is InvalidDataException)
            {
                throw new CorruptCompressedDataException(InvalidStreamMessage, ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
