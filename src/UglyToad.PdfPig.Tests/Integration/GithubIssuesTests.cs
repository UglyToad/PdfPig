namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;
    using DocumentLayoutAnalysis.PageSegmenter;
    using DocumentLayoutAnalysis.WordExtractor;
    using PdfPig.Core;

    public class GithubIssuesTests
    {
        [Fact]
        public void Issue1047()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Hang.pdf");

            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }));
            Assert.Equal("The cross reference was not found.", ex.Message);
        }

        [Fact]
        public void Issue1048()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("InvalidCast.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                Assert.NotNull(page.Letters);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                Assert.Single(blocks);
                Assert.Equal("hey, i'm a bug.", blocks[0].Text);
            }
        }

        [Fact]
        public void Issue554()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("2022.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page.Letters);

                    if (p < document.NumberOfPages)
                    {
                        Assert.NotEmpty(page.Letters);
                    }
                }
            }
        }

        [Fact]
        public void Issue822()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FileData_7.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    Assert.NotNull(page.Letters);
                }
            }
        }
        
        [Fact]
        public void Issue1040()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("pdfpig-issue-1040.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true}))
            {
                var page1 = document.GetPage(1);
                Assert.NotEmpty(page1.Letters);

                var page2 = document.GetPage(2);
                Assert.NotEmpty(page2.Letters);
            }
        }
        
        [Fact]
        public void Issue1013()
        {
            // NB: We actually do not fix issue 953 here, but another bug found with the same document.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("document_with_failed_fonts.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page2 = document.GetPage(2);
                Assert.NotEmpty(page2.Letters);

                var words2 = NearestNeighbourWordExtractor.Instance.GetWords(page2.Letters).ToArray();
                Assert.Equal("Doplňující", words2[0].Text);

                var page3 = document.GetPage(3);
                Assert.NotEmpty(page3.Letters);

                var words3 = NearestNeighbourWordExtractor.Instance.GetWords(page3.Letters).ToArray();
                Assert.Equal("Vinohradská", words3[8].Text);
            }
        }

        [Fact]
        public void Issue1016()
        {
            // Doc has letters with Shading pattern color

            var path = IntegrationHelpers.GetSpecificTestDocumentPath("colorcomparecrash.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page = document.GetPage(1);

                var letters = page.Letters;

                var firstLetter = letters[0];
                Assert.NotNull(firstLetter.Color);

                var secondLetter = letters[1];
                Assert.NotNull(secondLetter.Color);

                Assert.True(firstLetter.Color.Equals(secondLetter.Color));
            }
        }

        [Fact]
        public void Issue953()
        {
            // NB: We actually do not fix issue 953 here, but another bug found with the same document.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FailedToParseContentForPage32.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true}))
            {
                var page = document.GetPage(33);
                Assert.Equal(33, page.Number);
                Assert.Equal(792, page.Height);
                Assert.Equal(612, page.Width);
            }

            // Lenient parsing ON + Do not Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = false }))
            {
                var pageException = Assert.Throws<InvalidOperationException>(() =>  document.GetPage(33));
                Assert.Equal("Could not find the font with name /TT4 in the resource store. It has not been loaded yet.", pageException.Message);
            }

            var docException = Assert.Throws<PdfDocumentFormatException>(() =>
                PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false, SkipMissingFonts = false }));
            Assert.Equal("Could not find dictionary associated with reference in pages kids array: 102 0.", docException.Message);
        }

        [Fact]
        public void Issue953_IntOverflow()
        {
            // There is an integer overflow in Docstrum. We might want to fix that later on.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("FailedToParseContentForPage32.pdf");

            // Lenient parsing ON + Skip missing fonts
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true, SkipMissingFonts = true }))
            {
                var page = document.GetPage(13);
                Assert.Throws<OverflowException>(() => DocstrumBoundingBoxes.Instance.GetBlocks(page.GetWords()));
            }
        }

        [Fact]
        public void Issue987()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("zeroheightdemo.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToArray();
                foreach (var word in words)
                {
                    Assert.True(word.BoundingBox.Width > 0);
                    Assert.True(word.BoundingBox.Height > 0);
                }
            }
        }
        
        [Fact]
        public void Issue982()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("PDFBOX-659-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                for (int p = 1; p <= document.NumberOfPages; ++p)
                {
                    var page = document.GetPage(p);
                    foreach (var pdfImage in page.GetImages())
                    {
                        Assert.True(pdfImage.TryGetPng(out _));
                    }
                }
            }
        }

        [Fact]
        public void Issue973()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("JD5008.pdf");

            // Lenient parsing ON
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(2);
                Assert.NotNull(page);
                Assert.Equal(2, page.Number);
                Assert.NotEmpty(page.Letters);
            }

            // Lenient parsing OFF
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false }))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => document.GetPage(2));
                Assert.Equal("Cannot execute a pop of the graphics state stack, it would leave the stack empty.",
                    exception.Message);
            }
        }

        [Fact]
        public void Issue959()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("algo.pdf");

            // Lenient parsing ON
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                for (int i = 1; i <= document.NumberOfPages; ++i)
                {
                    var page = document.GetPage(i);
                    Assert.NotNull(page);
                    Assert.Equal(i, page.Number);
                }
            }

            // Lenient parsing OFF
            var exception = Assert.Throws<PdfDocumentFormatException>(() =>
                PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false }));

            Assert.Equal("The cross references formed an infinite loop.", exception.Message);
        }

        [Fact]
        public void Issue945()
        {
            // Odd ligatures names
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(2);
                Assert.Contains("ff", page.Letters.Select(l => l.Value));
            }

            path = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(7);
                Assert.Contains("fi", page.Letters.Select(l => l.Value));
            }

            path = IntegrationHelpers.GetDocumentPath("TIKA-2054-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(3);
                Assert.Contains("fi", page.Letters.Select(l => l.Value));

                page = document.GetPage(4);
                Assert.Contains("ff", page.Letters.Select(l => l.Value));

                page = document.GetPage(6);
                Assert.Contains("fl", page.Letters.Select(l => l.Value));

                page = document.GetPage(16);
                Assert.Contains("ffi", page.Letters.Select(l => l.Value));
            }
        }

        [Fact]
        public void Issue943()
        {
            var path = IntegrationHelpers.GetDocumentPath("MOZILLA-10225-0.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                Assert.NotNull(page);

                var letters = page.Letters;
                Assert.NotNull(letters);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

                Assert.Equal("Rocket and Spacecraft Propulsion", blocks[0].TextLines[0].Text);
                Assert.Equal("Principles, Practice and New Developments (Second Edition)", blocks[0].TextLines[1].Text);
            }
        }

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
