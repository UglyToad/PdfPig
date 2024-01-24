namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using PdfPig.Geometry;
    using System.Linq;
    using Xunit;

    public class EncodingsTests
    {
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
