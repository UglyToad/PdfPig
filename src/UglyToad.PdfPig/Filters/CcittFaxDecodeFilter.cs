namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Util;

    internal class CcittFaxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            //streamDictionary.TryGet<DictionaryToken>(NameToken.DecodeParms, out DictionaryToken parameters);
            //int cols = parameters.GetInt(NameToken.Columns);
            //int rows = parameters.GetInt(NameToken.Rows);
            //int height = streamDictionary.GetInt(NameToken.Height);

            //if (rows > 0 && height > 0)
            //{
            //    // PDFBOX-771, PDFBOX-3727: rows in DecodeParms sometimes contains an incorrect value
            //    rows = height;
            //}
            //else
            //{
            //    // at least one of the values has to have a valid value
            //    rows = Math.Max(rows, height);
            //}

            //int k = parameters.GetInt(NameToken.K);
            //bool encodedByteAlign = false;
            //if (parameters.TryGet(NameToken.EncodedByteAlign, out BooleanToken encodedByteAlignToken))
            //{
            //    encodedByteAlign = encodedByteAlignToken.Data;
            //}

            //int arraySize = (cols + 7) / 8 * rows;

            //byte[] decompressed = new byte[arraySize];

            //int type;
            //long tiffOptions = 0;
            //if (k == 0)
            //{
            //    type = 3; // Group 3 1D
            //    byte[] streamData = new byte[20];
            //    //int bytesRead = encoded.read(streamData);
            //    //if (bytesRead != streamData.Length)
            //    //{
            //    //    throw new EOFException("Can't read " + streamData.Length + " bytes");
            //    //}
            //    //encoded = new PushbackInputStream(encoded, streamData.Length);
            //    //((PushbackInputStream)encoded).unread(streamData);
            //    if (streamData[0] != 0 || (streamData[1] >> 4 != 1 && streamData[1] != 1))
            //    {
            //        // leading EOL (0b000000000001) not found, search further and try RLE if not
            //        // found
            //        type = 2;
            //        short b = (short)(((streamData[0] << 8) + (streamData[1] & 0xff)) >> 4);
            //        for (int i = 12; i < 160; i++)
            //        {
            //            b = (short)((b << 1) + ((streamData[(i / 8)] >> (7 - (i % 8))) & 0x01));
            //            if ((b & 0xFFF) == 1)
            //            {
            //                type = 3;
            //                break;
            //            }
            //        }
            //    }
            //}
            //else if (k > 0)
            //{
            //    // Group 3 2D
            //    type = 3;
            //    tiffOptions = 0x1;
            //}
            //else
            //{
            //    // Group 4
            //    type = 4;
            //}

            return input.ToArray();
        }
    }



}