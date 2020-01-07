namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    internal class Jbig2DecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The JBIG2 Filter for monochrome image data is not currently supported. " +
                                            "Try accessing the raw compressed data directly.");
        }
    }
}