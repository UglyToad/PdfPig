// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.Pdf.Tests.Parser.Parts
{
    using System;
    using IO;
    using Pdf.Parser.Parts;
    using Pdf.Util;
    using Xunit;

    public class BruteForceSearcherTests
    {
        [Fact]
        public void ReaderNull_Throws()
        {
            // ReSharper disable once ConvertToLocalFunction
            Action action = () => new BruteForceSearcher(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        private const string TestData = @"%PDF-1.5
%¿÷¢þ
2 0 obj
<< /Linearized 1 /L 26082 /H [ 722 130 ] /O 6 /E 25807 /N 1 /T 25806 >>
endobj
                                                                                                                 
3 0 obj
<< /Type /XRef /Length 58 /Filter /FlateDecode /DecodeParms << /Columns 4 /Predictor 12 >> /W [ 1 2 1 ] /Index [ 2 20 ] /Info 13 0 R /Root 4 0 R /Size 22 /Prev 25807                 /ID [<2ee88f3ee8a59b4041754ecb5960c518><2ee88f3ee8a59b4041754ecb5960c518>] >>
stream
xœcbdàg`b`8	$˜N€XF@‚±	DÜÌå@ÂÞ$›$$¦ƒXêLŒó~30A
endstream
endobj
4 0 obj
<< /Pages 14 0 R /Type /Catalog >>
endobj
5 0 obj
<< /Filter /FlateDecode /S 36 /Length 53 >>
stream
xœc```g``ºÄ
endstream
endobj
               
startxref
216
%%EOF";

        [Fact]
        public void SearcherFindsCorrectObjects()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(TestData);

            var reader = new RandomAccessBuffer(bytes);

            var searcher = new BruteForceSearcher(reader);

            var locations = searcher.GetObjectLocations();

            Assert.Equal(4, locations.Count);

            Assert.Equal(locations.Values, new long[]
            {
                TestData.IndexOf("2 0 obj", StringComparison.OrdinalIgnoreCase),
                TestData.IndexOf("3 0 obj", StringComparison.OrdinalIgnoreCase),
                TestData.IndexOf("4 0 obj", StringComparison.OrdinalIgnoreCase),
                TestData.IndexOf("5 0 obj", StringComparison.OrdinalIgnoreCase)
            });
        }

        [Fact]
        public void ReaderOnlyCallsOnce()
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(TestData);

            var reader = new ThrowingReader(new RandomAccessBuffer(bytes));

            var searcher = new BruteForceSearcher(reader);

            var locations = searcher.GetObjectLocations();

            Assert.Equal(4, locations.Count);

            reader.Throw = true;

            var newLocations = searcher.GetObjectLocations();

            Assert.Equal(4, locations.Count);

            foreach (var keyValuePair in locations)
            {
                Assert.Contains(newLocations.Keys, x => ReferenceEquals(x, keyValuePair.Key));
            }
        }
    }
}
