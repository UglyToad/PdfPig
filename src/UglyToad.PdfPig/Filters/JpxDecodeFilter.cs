namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    internal class JpxDecodeFilter : IFilter
    {
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The JPX Filter (JPEG2000) for image data is not currently supported. " +
                                            "Try accessing the raw compressed data directly.");
        }
    }
}