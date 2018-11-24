namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using Tokens;

    internal interface IFilter
    {
        byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex);
    }
}
