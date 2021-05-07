namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Decodes image data that has been encoded using either Group 3 or Group 4.
    ///
    /// Ported from https://github.com/apache/pdfbox/blob/714156a15ea6fcfe44ac09345b01e192cbd74450/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxFilter.java
    /// </summary>
    internal class CcittFaxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var decodeParms = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            var cols = decodeParms.GetIntOrDefault(NameToken.Columns, 1728);
            var rows = decodeParms.GetIntOrDefault(NameToken.Rows, 0);
            var height = streamDictionary.GetIntOrDefault(NameToken.Height, NameToken.H, 0);
            if (rows > 0 && height > 0)
            {
                // PDFBOX-771, PDFBOX-3727: rows in DecodeParms sometimes contains an incorrect value
                rows = height;
            }
            else
            {
                // at least one of the values has to have a valid value
                rows = Math.Max(rows, height);
            }

            var k = decodeParms.GetIntOrDefault(NameToken.K, 0);
            var encodedByteAlign = decodeParms.GetBooleanOrDefault(NameToken.EncodedByteAlign, false);
            var compressionType = DetermineCompressionType(input, k);
            using (var stream = new CcittFaxDecoderStream(new MemoryStream(input.ToArray()), cols, compressionType, encodedByteAlign))
            {
                var arraySize = (cols + 7) / 8 * rows;
                var decompressed = new byte[arraySize];
                ReadFromDecoderStream(stream, decompressed);

                // we expect black to be 1, if not invert the bitmap 
                var blackIsOne = decodeParms.GetBooleanOrDefault(NameToken.BlackIs1, false);
                if (!blackIsOne)
                {
                    InvertBitmap(decompressed);
                }

                return decompressed;
            }
        }

        private static CcittFaxCompressionType DetermineCompressionType(IReadOnlyList<byte> input, int k)
        {
            if (k == 0)
            {
                var compressionType = CcittFaxCompressionType.T4; // Group 3 1D

                if (input.Count < 20)
                {
                    throw new InvalidOperationException("The format is invalid");
                }

                if (input[0] != 0 || (input[1] >> 4 != 1 && input[1] != 1))
                {
                    // leading EOL (0b000000000001) not found, search further and
                    // try RLE if not found
                    compressionType = CcittFaxCompressionType.ModifiedHuffman;
                    var b = (short)(((input[0] << 8) + (input[1] & 0xff)) >> 4);
                    for (var i = 12; i < 160; i++)
                    {
                        b = (short)((b << 1) + ((input[(i / 8)] >> (7 - (i % 8))) & 0x01));
                        if ((b & 0xFFF) == 1)
                        {
                            return CcittFaxCompressionType.T4;
                        }
                    }
                }

                return compressionType;
            }
            else if (k > 0)
            {
                // Group 3 2D
                return CcittFaxCompressionType.T4;
            }
            else
            {
                return CcittFaxCompressionType.T6;
            }
        }

        private static void ReadFromDecoderStream(CcittFaxDecoderStream decoderStream, byte[] result)
        {
            int pos = 0;
            int read;
            while ((read = decoderStream.Read(result, pos, result.Length - pos)) > -1)
            {
                pos += read;
                if (pos >= result.Length)
                {
                    break;
                }
            }
            decoderStream.Close();
        }

        private static void InvertBitmap(byte[] bufferData)
        {
            for (int i = 0, c = bufferData.Length; i < c; i++)
            {
                bufferData[i] = (byte)(~bufferData[i] & 0xFF);
            }
        }
    }
}
