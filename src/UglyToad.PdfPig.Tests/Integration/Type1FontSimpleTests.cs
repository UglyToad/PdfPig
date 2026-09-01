namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Linq;

    public class Type1FontSimpleTests
    {
        [Fact]
        public void NoFontProgramUsesStandard14OrDescriptorBoundingBoxes()
        {
            var file = IntegrationHelpers.GetSpecificTestDocumentPath("type1-no-font-program.pdf");

            using (var document = PdfDocument.Open(file))
            {
                var letters = document.GetPage(1).Letters.ToArray();

                Assert.Equal("HgXY", string.Concat(letters.Select(x => x.Value)));
                Assert.All(letters, x => Assert.True(x.BoundingBox.Height > 0));

                // The PDF widths deliberately differ from Helvetica's AFM widths.
                Assert.Equal(18, letters[0].Width, 6);
                Assert.NotEqual(letters[0].Width, letters[0].BoundingBox.Width);
                Assert.Equal(18.2, letters[1].Width, 6);
                Assert.True(letters[1].BoundingBox.Bottom < letters[1].StartBaseLine.Y);

                // The custom unembedded font is not Standard 14, so its descriptor is used.
                Assert.Equal(14, letters[2].Width, 6);
                Assert.Equal(15, letters[2].BoundingBox.Height, 6);
                Assert.Equal(14.2, letters[3].Width, 6);
                Assert.Equal(15, letters[3].BoundingBox.Height, 6);
            }
        }

        [Fact]
        public void Issue807()
        {
            var file = IntegrationHelpers.GetDocumentPath("Diacritics_export.pdf");

            using (var document = PdfDocument.Open(file))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToArray();

                Assert.Equal(3, words.Length);
                Assert.Equal("Espinosa", words[0].Text);
                Assert.Equal("Spínola", words[1].Text);
                Assert.Equal("Moraña,", words[2].Text);
            }
        }
    }
}
