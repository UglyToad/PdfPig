namespace UglyToad.PdfPig.Util
{
    using System;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.XObjects;

    /// <summary>
    /// Helper class for IPdfImages.
    /// </summary>
    public static class PdfImageHelper
    {
        const short TIFF_BIGENDIAN = 0x4d4d;
        const short TIFF_LITTLEENDIAN = 0x4949;

        const int IfdLength = 10;
        const int HeaderLength = 10 + (IfdLength * 12 + 4);

        /// <summary>
        /// Parse the rawBytes of an XObjectImage to return a Tiff encoded byte[].
        /// </summary>
        public static bool TryGetTiff(this IPdfImage image, out byte[] bytes)
        {
            bytes = null;

            if (image == null || image.GetType() != typeof(XObjectImage))
            {
                return false;
            }
            if (!image.TryGetBytes(out var lazyBytes))
            {
                lazyBytes = image.RawBytes;
            }
            var pureBytes = lazyBytes.ToArray();

            var xImage = (XObjectImage)image;

            xImage.ImageDictionary.TryGet<DictionaryToken>(NameToken.DecodeParms, out DictionaryToken parameters);

            using (MemoryStream buffer = new MemoryStream(HeaderLength + pureBytes.Length))
            {
                // TIFF Header
                buffer.Write(BitConverter.GetBytes(BitConverter.IsLittleEndian ? TIFF_LITTLEENDIAN : TIFF_BIGENDIAN), 0, 2); // tiff_magic (big/little endianness)
                buffer.Write(BitConverter.GetBytes((uint)42), 0, 2);         // tiff_version
                buffer.Write(BitConverter.GetBytes((uint)8), 0, 4);          // first_ifd (Image file directory) / offset
                buffer.Write(BitConverter.GetBytes((uint)IfdLength), 0, 2); // ifd_length, number of tags (ifd entries)

                // Dictionary should be in order based on the TiffTag value
                WriteTiffTag(buffer, TiffTag.SUBFILETYPE, TiffType.LONG, 1, 0);
                WriteTiffTag(buffer, TiffTag.IMAGEWIDTH, TiffType.LONG, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.Width));
                WriteTiffTag(buffer, TiffTag.IMAGELENGTH, TiffType.LONG, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.Height));
                WriteTiffTag(buffer, TiffTag.BITSPERSAMPLE, TiffType.SHORT, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.BitsPerComponent));
                
                var kParam = parameters.GetInt(NameToken.K);
                if (kParam < 0)
                {
                    // CCITT Group 4 fax encoding.
                    WriteTiffTag(buffer, TiffTag.COMPRESSION, TiffType.SHORT, 1, (uint)Compression.CCITTFAX4);
                }
                else
                {
                    // CCITT Group 3 fax encoding.
                    WriteTiffTag(buffer, TiffTag.COMPRESSION, TiffType.SHORT, 1, (uint)Compression.CCITTFAX3);
                }

                var blackIs1 = false;
                if (parameters.TryGet(NameToken.BlackIs1, out BooleanToken blackIs1Token))
                {
                    blackIs1 = blackIs1Token.Data;
                }
                // BlackIsOne
                WriteTiffTag(buffer, TiffTag.PHOTOMETRIC, TiffType.SHORT, 1, blackIs1 ? (uint)1 : (uint)0);

                WriteTiffTag(buffer, TiffTag.STRIPOFFSETS, TiffType.LONG, 1, HeaderLength);
                WriteTiffTag(buffer, TiffTag.SAMPLESPERPIXEL, TiffType.SHORT, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.BitsPerComponent));
                WriteTiffTag(buffer, TiffTag.ROWSPERSTRIP, TiffType.LONG, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.Height));
                WriteTiffTag(buffer, TiffTag.STRIPBYTECOUNTS, TiffType.LONG, 1, (uint)xImage.ImageDictionary.GetInt(NameToken.Length));

                // Next IFD Offset
                buffer.Write(BitConverter.GetBytes((uint)0), 0, 4);

                buffer.Write(pureBytes, 0, pureBytes.Length);

                bytes = buffer.GetBuffer();
            }
            return true;
        }

        private static void WriteTiffTag(Stream stream, TiffTag tag, TiffType type, uint count, uint value)
        {
            if (stream == null)
            {
                return;
            }

            stream.Write(BitConverter.GetBytes((uint)tag), 0, 2);
            stream.Write(BitConverter.GetBytes((uint)type), 0, 2);
            stream.Write(BitConverter.GetBytes(count), 0, 4);
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        private enum TiffTag
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
        private enum TiffType : short
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

        private enum Compression
        {
            /// <summary>
            /// CCITT Group 3 fax encoding.
            /// </summary>
            CCITTFAX3 = 3,

            /// <summary>
            /// CCITT Group 4 fax encoding.
            /// </summary>
            CCITTFAX4 = 4,
        }
    }
}
