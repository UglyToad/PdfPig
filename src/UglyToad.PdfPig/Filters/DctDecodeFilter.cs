namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;

    /// <summary>
    /// DST (Discrete Cosine Transform) Filter indicates data is encoded in JPEG format.
    /// <para>This filter is not implemented and will not be used during parsing.</para>
    /// </summary>
    public sealed class DctDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = false;

        /// <inheritdoc />
        public ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            throw new NotSupportedException("The DST (Discrete Cosine Transform) Filter indicates data is encoded in JPEG format. " +
                                            "This filter is not currently supported but the raw data can be supplied to JPEG supporting libraries.");
        }
    }
}
