namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    internal class DctDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true; // actually this is only partially supported. 8bit only. 1 and 3 component only. Be better to provide the stream for inspection to answer correctly. 

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            //throw new NotSupportedException("The DST (Discrete Cosine Transform) Filter indicates data is encoded in JPEG format. " +
            //                                "This filter is not currently supported but the raw data can be supplied to JPEG supporting libraries.");

            var ab = input.ToArray();
            var jpg = Images.Jpg.Jpg.Parse(ab, streamDictionary);
            var decodedBytes = jpg.Data;
            return decodedBytes;
        }
    }
}
