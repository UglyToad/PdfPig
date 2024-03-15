namespace UglyToad.PdfPig.Tests.Geometry
{
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Tests.Integration;

    public class PdfPathExtensionsTests
    {
        [Fact]
        public void ContainsRectangleEvenOdd()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("path_ext_oddeven"),
                 new ParsingOptions() { ClipPaths = true }))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToList();

                foreach (var path in page.ExperimentalAccess.Paths)
                {
                    Assert.NotEqual(FillingRule.NonZeroWinding, path.FillingRule); // allow none and even-odd

                    foreach (var c in words.Where(w => path.Contains(w.BoundingBox)).ToList())
                    {
                        Assert.Equal("in", c.Text.Split("_").Last());
                        words.Remove(c);
                    }
                }

                foreach (var w in words)
                {
                    Assert.NotEqual("in", w.Text.Split("_").Last());
                }
            }
        }
    }
}
