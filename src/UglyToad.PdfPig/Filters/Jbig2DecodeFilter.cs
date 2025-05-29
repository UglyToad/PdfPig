namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;

    /// <summary>
    /// JBIG2 Filter for monochrome image data.
    /// <para>This filter is not implemented and will not be used during parsing.</para>
    /// </summary>
    public sealed class Jbig2DecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            throw new NotSupportedException("The JBIG2 Filter for monochrome image data is not currently supported. " +
                                            "Try accessing the raw compressed data directly.");
        }
    }
}