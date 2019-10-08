namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    internal class CcittFaxDecodeFilter : IFilter
    {
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The CCITT Fax Filter for image data is not currently supported. " +
                                            "Try accessing the raw compressed data directly.");
        }
    }
}