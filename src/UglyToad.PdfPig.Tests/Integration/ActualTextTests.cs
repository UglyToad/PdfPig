namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class ActualTextTests
    {
        // The document's cross-reference stream is itself Brotli compressed, so on a target
        // without a Brotli decoder it cannot be opened at all.
#if NET || NETSTANDARD2_1_OR_GREATER
        private const string FileName = "Brotli-Prototype-FileC.pdf";

        /// <summary>
        /// Page 4 separates its words with an en space glyph and gives each of them a
        /// <c>/Span &lt;&lt;/ActualText (\xFE\xFF\x00\x20)&gt;&gt; BDC</c> - a byte order mark
        /// followed by an ordinary space - so the extracted text reads with ordinary spaces.
        /// Reading that string as bytes instead turned every one of them into the four characters
        /// U+00FE, U+00FF, U+0000, U+0020, which is what copying the text gave back.
        /// </summary>
        [Fact]
        public void ReplacementTextIsReadAsTextRatherThanAsItsBytes()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath(FileName);

            using var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true, UseActualText = true });

            var text = string.Concat(document.GetPage(4).Letters.Select(l => l.Value));

            Assert.StartsWith("PDF Association — Well-Tagged PDF (WTPDF)11. Introduction"
                              + "PDF is a digital format for representing documents.", text);

            // The byte order mark, had it survived as two characters of its own.
            Assert.DoesNotContain("\u00fe\u00ff", text);
        }

        /// <summary>
        /// The contrast: without the option the glyphs themselves are extracted, which is where
        /// the en spaces the replacement text stands in for can be seen.
        /// </summary>
        [Fact]
        public void WithoutTheOptionTheGlyphsThemselvesAreExtracted()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath(FileName);

            using var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true, UseActualText = false });

            var text = string.Concat(document.GetPage(4).Letters.Select(l => l.Value));

            Assert.StartsWith("PDF Association — Well-Tagged PDF (WTPDF)11. Introduction"
                              + "PDF\u2002is\u2002a\u2002digital\u2002format", text);
        }
#endif
    }
}
