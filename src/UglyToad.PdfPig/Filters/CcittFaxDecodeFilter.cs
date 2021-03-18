namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;

    internal class CcittFaxDecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;
        
        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            return input.ToArray();
        }
    }



}