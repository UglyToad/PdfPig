namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Util;

    internal class CcittFaxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        const short TIFF_BIGENDIAN = 0x4d4d;
        const short TIFF_LITTLEENDIAN = 0x4949;

        const int IfdLength = 10;
        const int HeaderLength = 10 + (IfdLength * 12 + 4);

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var bytes = input.ToArray();

            var parameters = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);

            using (MemoryStream buffer = new MemoryStream(HeaderLength + bytes.Length))
            {
                // TIFF Header
                buffer.Write(BitConverter.GetBytes(BitConverter.IsLittleEndian ? TIFF_LITTLEENDIAN : TIFF_BIGENDIAN), 0, 2); // tiff_magic (big/little endianness)
                buffer.Write(BitConverter.GetBytes((uint)42), 0, 2);         // tiff_version
                buffer.Write(BitConverter.GetBytes((uint)8), 0, 4);          // first_ifd (Image file directory) / offset
                buffer.Write(BitConverter.GetBytes((uint)IfdLength), 0, 2); // ifd_length, number of tags (ifd entries)

                // Dictionary should be in order based on the TiffTag value
                WriteTiffTag(buffer, TiffTag.SUBFILETYPE, TiffType.LONG, 1, 0);
                WriteTiffTag(buffer, TiffTag.IMAGEWIDTH, TiffType.LONG, 1, (uint)streamDictionary.GetInt(NameToken.Width));
                WriteTiffTag(buffer, TiffTag.IMAGELENGTH, TiffType.LONG, 1, (uint)streamDictionary.GetInt(NameToken.Height));
                WriteTiffTag(buffer, TiffTag.BITSPERSAMPLE, TiffType.SHORT, 1, (uint)streamDictionary.GetInt(NameToken.BitsPerComponent));

                // CCITT Group 4 fax encoding.
                WriteTiffTag(buffer, TiffTag.COMPRESSION, TiffType.SHORT, 1, (uint)4); 

                var blackIs1 = false;
                if (parameters.TryGet(NameToken.BlackIs1, out BooleanToken blackIs1Token))
                {
                    blackIs1 = blackIs1Token.Data;
                }
                // BlackIsOne
                WriteTiffTag(buffer, TiffTag.PHOTOMETRIC, TiffType.SHORT, 1, blackIs1 ? (uint)1 : (uint)0); 

                WriteTiffTag(buffer, TiffTag.STRIPOFFSETS, TiffType.LONG, 1, HeaderLength);
                WriteTiffTag(buffer, TiffTag.SAMPLESPERPIXEL, TiffType.SHORT, 1, (uint)streamDictionary.GetInt(NameToken.BitsPerComponent));
                WriteTiffTag(buffer, TiffTag.ROWSPERSTRIP, TiffType.LONG, 1, (uint)streamDictionary.GetInt(NameToken.Height));
                WriteTiffTag(buffer, TiffTag.STRIPBYTECOUNTS, TiffType.LONG, 1, (uint)streamDictionary.GetInt(NameToken.Length));

                // Next IFD Offset
                buffer.Write(BitConverter.GetBytes((uint)0), 0, 4);

                buffer.Write(bytes, 0, bytes.Length);
                return (buffer.GetBuffer());
            }
        }

        private static void WriteTiffTag(Stream stream, TiffTag tag, TiffType type, uint count, uint value)
        {
            if (stream == null) {
                return;
            }

            stream.Write(BitConverter.GetBytes((uint)tag), 0, 2);
            stream.Write(BitConverter.GetBytes((uint)type), 0, 2);
            stream.Write(BitConverter.GetBytes(count), 0, 4);
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }
    }

    internal enum TiffTag
    {
        /// <summary>
        /// Subfile data descriptor.
        /// </summary>
        SUBFILETYPE = 254,

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        IMAGEWIDTH = 256,

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        IMAGELENGTH = 257,

        /// <summary>
        /// Bits per channel (sample).
        /// </summary>
        BITSPERSAMPLE = 258,

        /// <summary>
        /// Data compression technique.
        /// </summary>
        COMPRESSION = 259,

        /// <summary>
        /// Photometric interpretation.
        /// </summary>
        PHOTOMETRIC = 262,

        /// <summary>
        /// Offsets to data strips.
        /// </summary>
        STRIPOFFSETS = 273,

        /// <summary>
        /// Samples per pixel.
        /// </summary>
        SAMPLESPERPIXEL = 277,

        /// <summary>
        /// Rows per strip of data.
        /// </summary>
        ROWSPERSTRIP = 278,

        /// <summary>
        /// Bytes counts for strips.
        /// </summary>
        STRIPBYTECOUNTS = 279
    }
    internal enum TiffType : short
    {
        /// <summary>
        /// 16-bit unsigned integer.
        /// </summary>
        SHORT = 3,

        /// <summary>
        /// 32-bit unsigned integer.
        /// </summary>
        LONG = 4
    }

}