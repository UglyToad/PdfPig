namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// A filter is used in a PDF to encode/decode data either to compress it
    /// or derive an ASCII representation of the data.
    /// </summary>
    internal interface IFilter
    {
        /// <summary>
        /// Whether this library can decode information encoded using this filter.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Decodes data encoded using this filter type.
        /// </summary>
        /// <param name="input">The encoded bytes which were encoded using this filter.</param>
        /// <param name="streamDictionary">The dictionary of the <see cref="StreamToken"/> (or other dictionary types, e.g. inline images) containing these bytes.</param>
        /// <param name="filterIndex">The position of this filter in the pipeline used to encode data.</param>
        /// <returns>The decoded bytes.</returns>
        byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex);
    }
}
