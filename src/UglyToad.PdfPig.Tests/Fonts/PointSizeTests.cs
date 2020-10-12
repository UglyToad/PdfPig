namespace UglyToad.PdfPig.Tests.Fonts
{
    using UglyToad.PdfPig.Tests.Dla;
    using Xunit;

    public class PointSizeTests
    {
        [Fact]
        public void RotatedText()
        {
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath("complex rotated")))
            {
                var page = document.GetPage(1);

                foreach (var letter in page.Letters)
                {
                    Assert.Equal(12, letter.PointSize);
                }
            }
        }
    }
}
