namespace UglyToad.PdfPig.Tests
{
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    /// <summary>
    /// A filter provider which registers the standard <see cref="FlateFilter"/> under a non-standard name.
    /// <para>
    /// Because <see cref="DefaultFilterProvider"/> throws for the unknown name, any code path which decodes a
    /// stream filtered with <see cref="CustomFlateName"/> can only succeed if it was handed this provider. This
    /// makes it possible to assert that a user supplied <see cref="ParsingOptions.FilterProvider"/> is actually
    /// reaching the code path under test rather than being silently replaced by the default provider.
    /// </para>
    /// </summary>
    internal sealed class CustomNameFilterProvider : BaseFilterProvider
    {
        /// <summary>
        /// The non-standard filter name. It is deliberately the same length as <c>FlateDecode</c> so it can be
        /// substituted into an existing PDF without invalidating any cross reference offsets.
        /// </summary>
        public const string CustomFlateName = "PigTstFlate";

        private readonly CountingFilter counting;

        /// <summary>
        /// The number of times a stream filtered with <see cref="CustomFlateName"/> has been decoded.
        /// </summary>
        public int CustomFilterDecodeCount => counting.DecodeCount;

        private CustomNameFilterProvider(IReadOnlyDictionary<string, IFilter> filters, CountingFilter counting)
            : base(filters)
        {
            this.counting = counting;
        }

        public static CustomNameFilterProvider Create()
        {
            var counting = new CountingFilter(new FlateFilter());

            var ascii85 = new Ascii85Filter();
            var asciiHex = new AsciiHexDecodeFilter();
            var flate = new FlateFilter();
            var runLength = new RunLengthFilter();
            var lzw = new LzwFilter();

            var filters = new Dictionary<string, IFilter>
            {
                { CustomFlateName, counting },
                { NameToken.Ascii85Decode.Data, ascii85 },
                { NameToken.Ascii85DecodeAbbreviation.Data, ascii85 },
                { NameToken.AsciiHexDecode.Data, asciiHex },
                { NameToken.AsciiHexDecodeAbbreviation.Data, asciiHex },
                { NameToken.FlateDecode.Data, flate },
                { NameToken.FlateDecodeAbbreviation.Data, flate },
                { NameToken.RunLengthDecode.Data, runLength },
                { NameToken.RunLengthDecodeAbbreviation.Data, runLength },
                { NameToken.LzwDecode.Data, lzw },
                { NameToken.LzwDecodeAbbreviation.Data, lzw }
            };

            return new CustomNameFilterProvider(filters, counting);
        }

        public void ResetCount() => counting.Reset();

        /// <summary>
        /// Replaces every occurrence of <c>FlateDecode</c> in <paramref name="pdfBytes"/> with
        /// <see cref="CustomFlateName"/>. Both names are the same length so every byte offset in the file
        /// (cross reference table entries, <c>startxref</c>, stream lengths) remains valid.
        /// </summary>
        public static byte[] ReplaceFlateDecodeName(byte[] pdfBytes)
        {
            var from = OtherEncodings.StringAsLatin1Bytes(NameToken.FlateDecode.Data);
            var to = OtherEncodings.StringAsLatin1Bytes(CustomFlateName);

            Assert.Equal(from.Length, to.Length);

            var result = pdfBytes.ToArray();
            var replacements = 0;

            for (var i = 0; i <= result.Length - from.Length; i++)
            {
                var match = true;
                for (var j = 0; j < from.Length; j++)
                {
                    if (result[i + j] != from[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (!match)
                {
                    continue;
                }

                to.CopyTo(result, i);
                replacements++;
                i += from.Length - 1;
            }

            Assert.True(replacements > 0, "Expected the source document to use the FlateDecode filter.");

            return result;
        }

        private sealed class CountingFilter : IFilter
        {
            private readonly IFilter inner;

            public int DecodeCount { get; private set; }

            public CountingFilter(IFilter inner)
            {
                this.inner = inner;
            }

            public bool IsSupported => inner.IsSupported;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
            {
                DecodeCount++;
                return inner.Decode(input, streamDictionary, filterProvider, filterIndex);
            }

            public void Reset() => DecodeCount = 0;
        }
    }
}
