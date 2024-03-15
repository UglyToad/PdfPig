namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

    public class SinglePageSimpleOpenOfficeTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file), ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void HasCorrectLetterBoundingBoxes()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var comparer = new DoubleComparer(3d);

                Assert.Equal("I", page.Letters[0].Value);

                Assert.Equal(90.1d, page.Letters[0].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2d, page.Letters[0].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(94.0d, page.Letters[0].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(719.89d, page.Letters[0].GlyphRectangle.TopRight.Y, comparer);

                Assert.Equal("a", page.Letters[5].Value);

                Assert.Equal(114.5d, page.Letters[5].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2d, page.Letters[5].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(119.82d, page.Letters[5].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(714.89d, page.Letters[5].GlyphRectangle.TopRight.Y, comparer);

                Assert.Equal("f", page.Letters[16].Value);

                Assert.Equal(169.9d, page.Letters[16].GlyphRectangle.BottomLeft.X, comparer);
                Assert.Equal(709.2d, page.Letters[16].GlyphRectangle.BottomLeft.Y, comparer);

                Assert.Equal(176.89d, page.Letters[16].GlyphRectangle.TopRight.X, comparer);
                Assert.Equal(719.89d, page.Letters[16].GlyphRectangle.TopRight.Y, comparer);
            }
        }

        [Fact]
        public void GetsCorrectPageTextIgnoringHiddenCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.Equal("I am a simple pdf.", text);
            }
        }

        [Fact]
        public void TryGetBookmarksFalse()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                Assert.False(document.TryGetBookmarks(out _));
            }
        }

        [Fact]
        public void StartXRefNotNearEnd()
        {
            var bytes = File.ReadAllBytes(GetFilename());

            var emptyTrailer = new byte[2026];
            emptyTrailer[0] = 10;

            bytes = bytes.Concat(emptyTrailer).ToArray();

            using (var document = PdfDocument.Open(bytes, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }
    }
}
