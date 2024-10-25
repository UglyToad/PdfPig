namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;

    public class GithubIssuesTests
    {
        [Fact]
        public void Issue736()
        {
            var doc = IntegrationHelpers.GetDocumentPath("Approved_Document_B__fire_safety__volume_2_-_Buildings_other_than_dwellings__2019_edition_incorporating_2020_and_2022_amendments.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                Assert.True(document.TryGetBookmarks(out var bookmarks));
                Assert.Single(bookmarks.Roots);
                Assert.Equal(36, bookmarks.Roots[0].Children.Count);
            }
        }

        [Fact]
        public void Issue693()
        {
            var doc = IntegrationHelpers.GetDocumentPath("reference-2-numeric-error.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(1269, page1.Letters.Count);
            }
        }

        [Fact]
        public void Issue692()
        {
            var doc = IntegrationHelpers.GetDocumentPath("cmap-parsing-exception.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(796, page1.Letters.Count);
            }

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = false, SkipMissingFonts = false }))
            {
                var ex = Assert.Throws<InvalidOperationException>(() => document.GetPage(1));
                Assert.StartsWith("Read byte called on input bytes which was at end of byte set.", ex.Message);
            }
        }

        [Fact]
        public void Issue874()
        {
            var doc = IntegrationHelpers.GetDocumentPath("ErcotFacts.pdf");

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page1 = document.GetPage(1);
                Assert.Equal(1788, page1.Letters.Count);

                var page2 = document.GetPage(2);
                Assert.Equal(2430, page2.Letters.Count);
            }

            using (var document = PdfDocument.Open(doc, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = false }))
            {
                var ex = Assert.Throws<ArgumentNullException>(() => document.GetPage(1));
                Assert.StartsWith("Value cannot be null.", ex.Message);
            }
        }

        [Fact]
        public void Issue913()
        {
            var doc = IntegrationHelpers.GetSpecificTestDocumentPath("Rotation 45.pdf");

            using (var document = PdfDocument.Open(doc))
            {
                var page1 = document.GetPage(1);

                for (int l = 131; l <= 137; ++l)
                {
                    var letter = page1.Letters[l];
                    Assert.Equal(TextOrientation.Other, letter.TextOrientation);
                    Assert.Equal(45.0, letter.GlyphRectangle.Rotation, 5);
                }

                var page2 = document.GetPage(2);
                Assert.Equal(157, page2.Letters.Count);

                var page3 = document.GetPage(3);
                Assert.Equal(283, page3.Letters.Count);

                var page4 = document.GetPage(4);
                Assert.Equal(304, page4.Letters.Count);
            }
        }
    }
}
