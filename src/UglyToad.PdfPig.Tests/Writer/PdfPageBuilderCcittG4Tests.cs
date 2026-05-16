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

    public class PdfPageBuilderCcittG4Tests
    {
        [Fact]
        public void FixtureProvidesRawCcittData()
        {
            var fixture = CcittG4TestImage.Load();

            Assert.True(fixture.Width > 0);
            Assert.True(fixture.Height > 0);
            Assert.NotEmpty(fixture.RawCcittData);
            Assert.False(fixture.BlackIs1);
        }

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

        [Fact]
        public void FixtureWritesDecodeZeroOne()
        {
            var fixture = CcittG4TestImage.Load();
            var image = BuildFaxImage(fixture.BlackIs1);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            // The generated fixture comes from a TIFF with Photometric.MINISWHITE, which must map to
            // blackIs1 = false for AddCcittG4. Reversing this to match TIFF naming reintroduces negative PDFs.
            Assert.Equal([0, 1], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
        }

        [Fact]
        public void AddCcittG4WithBlackIs1TrueWritesDecodeOneZero()
        {
            var image = BuildFaxImage(blackIs1: true);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            Assert.Equal([1, 0], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
            Assert.True(image.ImageDictionary.TryGet(NameToken.DecodeParms, out DictionaryToken? decodeParms));
            Assert.False(decodeParms!.Data.ContainsKey(NameToken.BlackIs1));
        }

        [Fact]
        public void AddCcittG4WithBlackIs1FalseWritesDecodeZeroOne()
        {
            var image = BuildFaxImage(blackIs1: false);

            var decode = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.Decode]);

            Assert.Equal([0, 1], decode.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
            Assert.True(image.ImageDictionary.TryGet(NameToken.DecodeParms, out DictionaryToken? decodeParms));
            Assert.False(decodeParms!.Data.ContainsKey(NameToken.BlackIs1));
        }

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
