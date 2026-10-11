namespace UglyToad.PdfPig.Tests.Writer
{
    using System.IO;
    using UglyToad.PdfPig.Content;
    using System.Linq;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Tests.Writer.TestImages;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Writer;
    using Xunit;

    /// <summary>Tests CCITT Group 4 image metadata written by PdfPageBuilder.</summary>
    /// <remarks>These tests inspect the generated PDF image dictionary, not decoded pixels.
    /// The writer's blackIs1 argument chooses the image Decode array and leaves BlackIs1
    /// absent from DecodeParms. Decode maps sample values to grayscale: [0, 1] preserves
    /// their order and [1, 0] reverses it. The TIFF fixture supplies a known polarity.</remarks>
    public class PdfPageBuilderCcittG4Tests
    {
        /// <summary>Checks that the fixture supplies compressed bytes, positive dimensions and its expected polarity.</summary>
        [Fact]
        public void FixtureProvidesRawCcittData()
        {
            var fixture = CcittG4TestImage.Load();

            Assert.True(fixture.Width > 0);
            Assert.True(fixture.Height > 0);
            Assert.NotEmpty(fixture.RawCcittData);
            Assert.False(fixture.BlackIs1);
        }

        /// <summary>Writes a CCITT image into a page and reads back its dimensions and one-bit sample metadata.</summary>
        [Fact]
        public void AddCcittG4AddsFaxImage()
        {
            var fixture = CcittG4TestImage.Load();

            byte[] pdfBytes;
            using (var documentBuilder = new PdfDocumentBuilder())
            {
                var pageBuilder = documentBuilder.AddPage(fixture.Width, fixture.Height);

                pageBuilder.AddCcittG4(fixture.RawCcittData, fixture.Width, fixture.Height, blackIs1: fixture.BlackIs1);

                pdfBytes = documentBuilder.Build();
            }

            File.WriteAllBytes(@"PdfPageBuilderTests_CanAddTifCcittG4.pdf", pdfBytes);

            using (var document = PdfDocument.Open(pdfBytes))
            {
                var page = document.GetPage(1);
                var image = Assert.Single(page.GetImages());

                Assert.Equal(fixture.Width, image.WidthInSamples);
                Assert.Equal(fixture.Height, image.HeightInSamples);
                Assert.Equal(1, image.BitsPerComponent);
            }
        }

        /// <summary>Preserves the TIFF fixture polarity through the writer's grayscale Decode mapping.</summary>
        [Fact]
        public void FixtureWritesDecodeZeroOne()
        {
            var fixture = CcittG4TestImage.Load();
            var image = BuildFaxImage(fixture.BlackIs1);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            // This MINISWHITE TIFF fixture declares blackIs1=false. Its expected [0, 1]
            // mapping preserves the fixture polarity; swapping the entries would invert the image.
            Assert.Equal([0, 1], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
        }

        /// <summary>Uses the reversed Decode mapping for blackIs1=true without setting the filter BlackIs1 parameter.</summary>
        [Fact]
        public void AddCcittG4WithBlackIs1TrueWritesDecodeOneZero()
        {
            var image = BuildFaxImage(blackIs1: true);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            Assert.Equal([1, 0], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
            Assert.True(image.ImageDictionary.TryGet(NameToken.DecodeParms, out DictionaryToken? decodeParms));
            Assert.False(decodeParms!.Data.ContainsKey(NameToken.BlackIs1));
        }

        /// <summary>Uses the ordered Decode mapping for blackIs1=false without setting the filter BlackIs1 parameter.</summary>
        [Fact]
        public void AddCcittG4WithBlackIs1FalseWritesDecodeZeroOne()
        {
            var image = BuildFaxImage(blackIs1: false);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            Assert.Equal([0, 1], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
            Assert.True(image.ImageDictionary.TryGet(NameToken.DecodeParms, out DictionaryToken? decodeParms));
            Assert.False(decodeParms!.Data.ContainsKey(NameToken.BlackIs1));
        }

        /// <summary>Writes the fixture with the requested polarity and returns its image metadata for assertions.</summary>
        private static IPdfImage BuildFaxImage(bool blackIs1)
        {
            var fixture = CcittG4TestImage.Load();

            byte[] pdfBytes;
            using (var documentBuilder = new PdfDocumentBuilder())
            {
                var pageBuilder = documentBuilder.AddPage(fixture.Width, fixture.Height);

                pageBuilder.AddCcittG4(fixture.RawCcittData, fixture.Width, fixture.Height, blackIs1: blackIs1);

                pdfBytes = documentBuilder.Build();
            }

            using var document = PdfDocument.Open(pdfBytes);
            var page = document.GetPage(1);

            return Assert.Single(page.GetImages());
        }
    }
}