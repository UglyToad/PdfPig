using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace UglyToad.PdfPig.Tests.Integration
{
    public class OpenTypeFontTests
    {
        [Fact]
        public void Issue672()
        {
            // NB: The issue is actually not fully fixed: the change are just allowing
            // to parse the document and get the text without error
            // but the embedded font data is not properly parsed.
            // It seems the font bytes are incorrectly parsed using the TrueTypeFontParser
            // and are actually parsable with CompactFontFormatParser, but with some errors though.

            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Why.does.this.not.work")))
            {
                var page = document.GetPage(1);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);

                var lines = DocstrumBoundingBoxes.Instance.GetBlocks(words).SelectMany(b => b.TextLines).ToArray();

                Assert.Equal(3, lines.Length);

                Assert.Equal("THIS TEST SEEMS TO BREAK THE PARSER....", lines[0].Text);
                Assert.Equal("This is just some test text.", lines[1].Text);
                Assert.Equal("SO DOES THIS", lines[2].Text);
            }
        }

        [Fact]
        public void Issue672ok()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Test.Doc")))
            {
                var page = document.GetPage(1);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);

                var lines = DocstrumBoundingBoxes.Instance.GetBlocks(words).SelectMany(b => b.TextLines).ToArray();

                Assert.Equal(4, lines.Length);

                Assert.Equal("This is just a bunch of boring text...", lines[0].Text);
                Assert.Equal("THIS IS SOME SEMPLICITA PRO FONT", lines[1].Text);
                Assert.Equal("Hopefully font that are not embedded on the server.", lines[2].Text);
                Assert.Equal("And a bit of Verdana for good measure.", lines[3].Text);
            }
        }

        [Fact]
        public void So74165171()
        {
            // https://stackoverflow.com/questions/74165171/embedded-opentype-cff-font-in-a-pdf-shows-strange-behaviour-in-some-viewers

            // Adding this test case as the OpenType font is correctly parsed using TrueTypeFontParser
            // It seems there are further issues with the extracted test (also the case in Acrobat Reader).
            // Out of scope for the moment

            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("test-2_so_74165171")))
            {
                var page = document.GetPage(1);

                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();

                Assert.Equal(2, words.Length);
            }
        }
    }
}
