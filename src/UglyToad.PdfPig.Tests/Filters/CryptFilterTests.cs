namespace UglyToad.PdfPig.Tests.Filters
{
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class CryptFilterTests
    {
        [Fact]
        public void StreamWithOnlyCryptFilterDecodesToItsData()
        {
            // Unencrypted XMP metadata in an encrypted document (/EncryptMetadata false), as written by some producers.
            var data = "<x:xmpmeta/>"u8.ToArray();
            var stream = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, new ArrayToken([NameToken.Crypt]) },
                { NameToken.Type, NameToken.Metadata }
            }), data);

            var decoded = stream.Decode(DefaultFilterProvider.Instance);

            Assert.Equal(data, decoded.ToArray());
        }

        [Fact]
        public void CryptFilterKeepsTheDecodeParmsOfLaterFilters()
        {
            var stream = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, new ArrayToken([NameToken.Crypt, NameToken.AsciiHexDecode]) },
                {
                    NameToken.DecodeParms, new ArrayToken([
                        new DictionaryToken(new Dictionary<NameToken, IToken>
                        {
                            { NameToken.Type, NameToken.Create("CryptFilterDecodeParms") },
                            { NameToken.Name, NameToken.Identity }
                        }),
                        NullToken.Instance
                    ])
                }
            }), "48656C6C6F>"u8.ToArray());

            var decoded = stream.Decode(DefaultFilterProvider.Instance);

            Assert.Equal("Hello"u8.ToArray(), decoded.ToArray());
        }

        [Fact]
        public void ProviderWithoutCryptFilterStillResolvesIt()
        {
            var provider = new NoCryptFilterProvider();

            var filters = provider.GetNamedFilters([NameToken.Crypt]);

            Assert.Single(filters);
            Assert.True(filters[0].IsSupported);
        }

        private sealed class NoCryptFilterProvider() : BaseFilterProvider(new Dictionary<string, IFilter>());
    }
}
