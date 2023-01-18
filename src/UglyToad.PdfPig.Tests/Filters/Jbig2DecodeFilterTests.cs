using System.Collections.Generic;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tests.Images;
using UglyToad.PdfPig.Tokens;
using Xunit;

namespace UglyToad.PdfPig.Tests.Filters
{
    public class Jbig2DecodeFilterTests
    {
        [Fact]
        public void CanDecodeJbig2CompressedImageData_WithoutGlobalSegments()
        {
            var encodedImageBytes = ImageHelpers.LoadFileBytes("sampledata_page1.jb2");

            var filter = new Jbig2DecodeFilter();
            var dictionary = new Dictionary<NameToken, IToken>()
            {
                { NameToken.Filter, NameToken.Jbig2Decode }
            };

            var expectedBytes = ImageHelpers.LoadFileBytes("sampledata_page1.jb2-decoded.bin");
            var decodedBytes = filter.Decode(encodedImageBytes, new DictionaryToken(dictionary), 0);
            Assert.Equal(expectedBytes, decodedBytes);
        }

        [Fact]
        public void CanDecodeJbig2CompressedImageData_WithGlobalSegments()
        {
            var encodedGlobalsBytes = ImageHelpers.LoadFileBytes("globals.jb2");
            var encodedImageBytes = ImageHelpers.LoadFileBytes("img-refs-globals.jb2");

            var filter = new Jbig2DecodeFilter();
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.Jbig2Decode },
                { NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Jbig2Globals, new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>()), encodedGlobalsBytes) }
                    })
                },
                { NameToken.ImageMask, BooleanToken.True }
            };

            var expectedBytes = ImageHelpers.LoadFileBytes("img-refs-globals-decoded.bin", isCompressed: true);
            var decodedBytes = filter.Decode(encodedImageBytes, new DictionaryToken(dictionary), 0);
            Assert.Equal(expectedBytes, decodedBytes);
        }
    }
}
