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

                return Decompress(input, predictor, colors, bitsPerComponent, columns);
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
            int columns)
        {
            using var memoryStream = MemoryHelper.AsReadOnlyMemoryStream(input);
            // The first 2 bytes are the header which DeflateStream does not support.
            memoryStream.ReadByte();
            memoryStream.ReadByte();

            using var output = new MemoryStream((int)(input.Length * 1.5));

            using (var deflate = new DeflateStream(memoryStream, CompressionMode.Decompress))
            using (var f = PngPredictor.WrapPredictor(output, predictor, colors, bitsPerComponent, columns))
            {
                // Copied a block at a time rather than with CopyTo, because a damaged
                // stream throws out of the read and CopyTo would discard the whole buffer
                // it was filling. Keeping each block that did inflate is what PDFBox's
                // FlateFilterDecoderStream does, and it is worth a lot on a large stream.
                var block = ArrayPool<byte>.Shared.Rent(BlockLength);

                try
                {
                    while (true)
                    {
                        int read;

                        try
                        {
                            read = deflate.Read(block, 0, block.Length);
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

                        f.Write(block, 0, read);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(block);
                }

                f.Flush();
            }

            return output.AsMemory();
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