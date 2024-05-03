﻿namespace UglyToad.PdfPig.Filters
{
    using Tokens;

    internal class DctDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The DST (Discrete Cosine Transform) Filter indicates data is encoded in JPEG format. " +
                                            "This filter is not currently supported but the raw data can be supplied to JPEG supporting libraries.");
        }
    }
}
