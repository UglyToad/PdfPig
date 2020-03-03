// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Parser.Parts
{
    using System;
    using System.IO;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Parser.Parts;
    using Xunit;

    public class BruteForceSearcherTests
    {
        private const string TestData = @"%PDF-1.5
%¿÷¢þ
2 17 obj
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

        private static readonly long[] TestDataOffsets = 
        {
            TestData.IndexOf("2 17 obj", StringComparison.OrdinalIgnoreCase),
            TestData.IndexOf("3 0 obj", StringComparison.OrdinalIgnoreCase),
            TestData.IndexOf("4 0 obj", StringComparison.OrdinalIgnoreCase),
            TestData.IndexOf("5 0 obj", StringComparison.OrdinalIgnoreCase)
        };

        [Fact]
        public void ReaderNull_Throws()
        {
            Action action = () => BruteForceSearcher.GetObjectLocations(null);

            Assert.Throws<ArgumentNullException>(action);
        }


        [Fact]
        public void SearcherFindsCorrectObjects()
        {
            var input = new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(TestData));
            
            var locations = BruteForceSearcher.GetObjectLocations(input);

            Assert.Equal(4, locations.Count);

            Assert.Equal(TestDataOffsets, locations.Values);
        }

        [Fact]
        public void ReaderOnlyCallsOnce()
        {
            var reader = StringBytesTestConverter.Convert(TestData, false);
            
            var locations = BruteForceSearcher.GetObjectLocations(reader.Bytes);

            Assert.Equal(4, locations.Count);
            
            var newLocations = BruteForceSearcher.GetObjectLocations(reader.Bytes);

            Assert.Equal(4, locations.Count);

            foreach (var keyValuePair in locations)
            {
                Assert.Contains(newLocations.Keys, x => x.Equals(keyValuePair.Key));
            }
        }

        [Fact]
        public void BruteForceSearcherFileOffsetsCorrect()
        {
            using (var fs = File.OpenRead(IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf")))
            {
                var bytes = new StreamInputBytes(fs);

                var locations = BruteForceSearcher.GetObjectLocations(bytes);

                Assert.Equal(13, locations.Count);

                Assert.Equal(6183, locations[new IndirectReference(1, 0)]);
                Assert.Equal(244, locations[new IndirectReference(2, 0)]);
                Assert.Equal(15, locations[new IndirectReference(3, 0)]);
                Assert.Equal(222, locations[new IndirectReference(4, 0)]);
                Assert.Equal(5766, locations[new IndirectReference(5, 0)]);
                Assert.Equal(353, locations[new IndirectReference(6, 0)]);
                Assert.Equal(581, locations[new IndirectReference(7, 0)]);
                Assert.Equal(5068, locations[new IndirectReference(8, 0)]);
                Assert.Equal(5091, locations[new IndirectReference(9, 0)]);
                
                var s = GetStringAt(bytes, locations[new IndirectReference(3, 0)]);
                Assert.StartsWith("3 0 obj", s);
            }
        }

        [Fact]
        public void BruteForceSearcherFileOffsetsCorrectOpenOffice()
        {
            var bytes = new ByteArrayInputBytes(File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf")));

            var locations = BruteForceSearcher.GetObjectLocations(bytes);

            Assert.Equal(13, locations.Count);

            Assert.Equal(17, locations[new IndirectReference(1, 0)]);
            Assert.Equal(249, locations[new IndirectReference(2, 0)]);
            Assert.Equal(14291, locations[new IndirectReference(3, 0)]);
            Assert.Equal(275, locations[new IndirectReference(4, 0)]);
            Assert.Equal(382, locations[new IndirectReference(5, 0)]);
            Assert.Equal(13283, locations[new IndirectReference(6, 0)]);
            Assert.Equal(13309, locations[new IndirectReference(7, 0)]);
            Assert.Equal(13556, locations[new IndirectReference(8, 0)]);
            Assert.Equal(13926, locations[new IndirectReference(9, 0)]);
            Assert.Equal(14183, locations[new IndirectReference(10, 0)]);
            Assert.Equal(14224, locations[new IndirectReference(11, 0)]);
            Assert.Equal(14428, locations[new IndirectReference(12, 0)]);
            Assert.Equal(14488, locations[new IndirectReference(13, 0)]);

            var s = GetStringAt(bytes, locations[new IndirectReference(12, 0)]);
            Assert.StartsWith("12 0 obj", s);
        }

        [Fact]
        public void BruteForceSearcherCorrectlyFindsAllObjectsWhenOffset()
        {
            var input = new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(TestData));

            input.Seek(593);

            var locations = BruteForceSearcher.GetObjectLocations(input);

            Assert.Equal(TestDataOffsets, locations.Values);
        }

        private static string GetStringAt(IInputBytes bytes, long location)
        {
            bytes.Seek(location);
            var txt = new byte[10];
            bytes.Read(txt);

            var s = OtherEncodings.BytesAsLatin1String(txt);

            return s;
        }
    }
}
