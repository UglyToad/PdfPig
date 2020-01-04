namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Logging;
    using Tokens;
    using Util;

    /// <inheritdoc />
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
    internal class FlateFilter : IFilter
    {
        // Defaults are from table 3.7 in the spec (version 1.7)
        private const int DefaultColors = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColumns = 1;

        private const byte Deflate32KbWindow = 120;
        private const byte ChecksumBits = 1;

        private readonly IDecodeParameterResolver decodeParameterResolver;
        private readonly IPngPredictor pngPredictor;
        private readonly ILog log;

        public FlateFilter(IDecodeParameterResolver decodeParameterResolver, IPngPredictor pngPredictor, ILog log)
        {
            this.decodeParameterResolver = decodeParameterResolver;
            this.pngPredictor = pngPredictor;
            this.log = log;
        }

        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var parameters = decodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var predictor = parameters.GetIntOrDefault(NameToken.Predictor, -1);

            var bytes = input.ToArray();
            try
            {
                var decompressed = Decompress(bytes);

                if (predictor == -1)
                {
                    return decompressed;
                }

                var colors = Math.Min(parameters.GetIntOrDefault(NameToken.Colors, DefaultColors), 32);
                var bitsPerComponent = parameters.GetIntOrDefault(NameToken.BitsPerComponent, DefaultBitsPerComponent);
                var columns = parameters.GetIntOrDefault(NameToken.Columns, DefaultColumns);

                var result = pngPredictor.Decode(decompressed, predictor, colors, bitsPerComponent, columns);

                return result;
            }
            catch (Exception ex)
            {
                log.Error("Could not decode a flate stream due to an error.", ex);
            }

            return bytes;
        }

        private byte[] Decompress(byte[] input)
        {
            try
            {
                using (var memoryStream = new MemoryStream(input))
                using (var output = new MemoryStream())
                {
                    // The first 2 bytes are the header which DeflateStream does not support.
                    memoryStream.ReadByte();
                    memoryStream.ReadByte();

                    using (var deflate = new DeflateStream(memoryStream, CompressionMode.Decompress))
                    {
                        deflate.CopyTo(output);
                        return output.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Error("Could not decode the input using the deflate stream. Input was: " + input, ex);
                throw;
            }
        }

        public byte[] Encode(Stream input, DictionaryToken streamDictionary, int index)
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
            {
                compressor.Write(data, 0, data.Length);
                compressor.Close();

                var compressed = compressStream.ToArray();

                var result = new byte[headerLength + compressed.Length + checksumLength];

                // Write the ZLib header.
                result[0] = Deflate32KbWindow;
                result[1] = ChecksumBits;

                // Write the compressed data.
                Array.Copy(compressed, 0, result, headerLength, compressed.Length);

                // Write Checksum of raw data.
                var checksum = Adler32Checksum.Calculate(data);

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