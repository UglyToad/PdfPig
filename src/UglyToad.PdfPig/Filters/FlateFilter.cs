namespace UglyToad.PdfPig.Filters
{
    using Fonts;
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.IO.Compression;
    using Tokens;
    using UglyToad.PdfPig.Core;
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

                var length = parameters.GetIntOrDefault(NameToken.Length, -1);

                if (length > 0 && length < input.Length)
                {
                    // Truncates final "\r\n" or "\n" from source data if any. Fixes detecting where the adler checksum is. (Zlib uses framing for this)
                    input = input.Slice(0, length);
                }

                return Decompress(input, predictor, colors, bitsPerComponent, columns);
            }
            catch
            {
                // ignored.
            }

            return input;
        }

        private static Memory<byte> Decompress(Memory<byte> input, int predictor, int colors, int bitsPerComponent, int columns)
        {
#if NET
            using var memoryStream = MemoryHelper.AsReadOnlyMemoryStream(input);
            try
            {
                using (var zlib = new ZLibStream(memoryStream, CompressionMode.Decompress))
                using (var output = new MemoryStream((int)(input.Length * 1.5)))
                using (var f = PngPredictor.WrapPredictor(output, predictor, colors, bitsPerComponent, columns))
                {
                    zlib.CopyTo(f);
                    f.Flush();

                    return output.AsMemory();
                }
            }
            catch (InvalidDataException ex)
            {
                throw new CorruptCompressedDataException("Invalid Flate compressed stream encountered", ex);
            }
#else
            // Ideally we would like to use the ZLibStream class but that is only available in .NET 5+.
            // We look at the raw data now
            // *  First we have 2 bytes, specifying the type of compression
            // * Then we have the deflated data
            // * Then we have a 4 byte checksum (Adler32)

            // Would be so nice to have zlib do the framing here... but the deflate stream already reads data from the stream that we need.

            using var memoryStream = MemoryHelper.AsReadOnlyMemoryStream(input.Slice(2, input.Length - 2 /* Header */ - 4 /* Checksum */));
            // The first 2 bytes are the header which DeflateStream can't handle. After the s
            var adlerBytes = input.Slice(input.Length - 4, 4).Span;
            uint expected = BinaryPrimitives.ReadUInt32BigEndian(adlerBytes);
            uint altExpected = expected;

            // Sometimes the data ends with "\r\n", "\r" or "\n" and we don't know if it is part of the zlib
            // Ideally this would have been removed by the caller from the provided length...
            if (adlerBytes[3] == '\n' || adlerBytes[3] == '\r')
            {
                if (adlerBytes[3] == '\n' && adlerBytes[2] == '\r')
                {
                    // Now we don't know which value is the good one. The value could be ok, or padding.
                    // Lets allow both values for now. Allowing two out of 2^32 is much better than allowing everything
                    adlerBytes = input.Slice(input.Length - 6, 4).Span;
                }
                else
                {
                    // Same but now for just '\n' or '\r' instead of '\r\n'
                    adlerBytes = input.Slice(input.Length - 5, 4).Span;
                }

                altExpected = BinaryPrimitives.ReadUInt32BigEndian(adlerBytes);
            }


            try
            {
                using (var deflate = new DeflateStream(memoryStream, CompressionMode.Decompress))
                using (var adlerStream = new Adler32ChecksumStream(deflate))
                using (var output = new MemoryStream((int)(input.Length * 1.5)))
                using (var f = PngPredictor.WrapPredictor(output, predictor, colors, bitsPerComponent, columns))
                {
                    adlerStream.CopyTo(f);
                    f.Flush();

                    uint actual = adlerStream.Checksum;
                    if (expected != actual && altExpected != actual)
                    {
                        throw new CorruptCompressedDataException("Flate stream has invalid checksum");
                    }

                    return output.AsMemory();
                }
            }
            catch (InvalidDataException ex)
            {
                throw new CorruptCompressedDataException("Invalid Flate compressed stream encountered", ex);
            }
#endif
        }

        /// <inheritdoc />
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