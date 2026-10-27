namespace UglyToad.PdfPig.Filters
{
    using System;
    using Tokens;

    /// <summary>
    /// The Crypt filter (PDF 1.5, 7.4.10) selects which crypt filter the security handler uses for a stream.
    /// Decryption is done by the security handler when the stream is read, so in the decode pipeline this filter
    /// leaves the data unchanged.
    /// </summary>
    internal sealed class CryptFilter : IFilter
    {
        public static readonly CryptFilter Instance = new CryptFilter();

        /// <inheritdoc />
        public bool IsSupported => true;

        /// <inheritdoc />
        public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
        {
            return input;
        }
    }
}
