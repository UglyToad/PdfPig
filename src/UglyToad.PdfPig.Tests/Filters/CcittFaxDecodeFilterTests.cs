namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Tests.Images;
    using UglyToad.PdfPig.Tokens;
    using Xunit;

    public class CcittFaxDecodeFilterTests
    {
        [Fact]
        public void CanDecodeCCittFaxCompressedImageData()
        {
            var encodedBytes = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");

            var filter = new CcittFaxDecodeFilter();
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.D, new ArrayToken(new []{ new NumericToken(1), new NumericToken(0) })},
                { NameToken.W, new NumericToken(1800) },
                { NameToken.H, new NumericToken(3113) },
                { NameToken.Bpc, new NumericToken(1) },
                { NameToken.F, NameToken.CcittfaxDecode },
                { NameToken.DecodeParms,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.K, new NumericToken(-1) },
                        { NameToken.Columns, new NumericToken(1800) },
                        { NameToken.Rows, new NumericToken(3113) },
                        { NameToken.BlackIs1, BooleanToken.True }
                    })
                }
            };

            var expectedBytes = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
            var decodedBytes = filter.Decode(encodedBytes, new DictionaryToken(dictionary), 0);
            Assert.Equal(expectedBytes, decodedBytes);
        }
    }
}
