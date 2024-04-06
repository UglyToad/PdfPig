namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using PdfPig.Geometry;

    public class EncodingsTests
    {
        [Fact]
        public void Windows1252Encoding()
        {
            string path = IntegrationHelpers.GetDocumentPath("GHOSTSCRIPT-698363-0.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                string actual = string.Concat(page.Letters.Select(l => l.Value));

                // The expected string value is just here to make sure we have the same results across net versions.
                // Feel free to correct/update it if chars are not actually correct.
                string expected = "ҘҹЧѥЧКጹѝঐܮ̂ҥ҇ҁӃ࿋\u0c0dҀғҊ˺෨ཌආр෨ཌ̂ҘҹЧѥЧКጹѝঐܮ̂ҥ҇ҁӃ࿋\u0c0dҀғҊ˺෨ཌආр෨ཌ̂ݰႺംࢥ༢࣭\u089aѽ̔ҫһҐ̔ݰႺംࢥ༢࣭\u089aѽ̔ҫһҐ̔";
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Issue688()
        {
            string path = IntegrationHelpers.GetDocumentPath("Indexed-DeviceRGB-JPXDecode-0-.0.0.255.0.-Font-F1_1_missing_char_255-1.pdf");
            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                var letters = page.Letters;
                Assert.NotEmpty(letters);

                var rect = new PdfRectangle(207, 158, 229, 168.5);

                var missingChars = letters.Where(l => rect.Contains(l.GlyphRectangle)).ToArray();

                Assert.NotEmpty(missingChars);
                Assert.True(missingChars.Length == 2);

                Assert.Equal("先", missingChars[0].Value);
                Assert.Equal("祖", missingChars[1].Value);
            }
        }
    }
}
