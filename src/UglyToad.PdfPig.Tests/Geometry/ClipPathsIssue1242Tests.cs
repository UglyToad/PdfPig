namespace UglyToad.PdfPig.Tests.Geometry
{
    using System.Linq;
    using UglyToad.PdfPig.Tests.Integration;
    using Xunit;

    public class ClipPathsIssue1242Tests
    {
        /// <summary>
        /// Regression test for https://github.com/UglyToad/PdfPig/issues/1242.
        /// With <see cref="ParsingOptions.ClipPaths"/> enabled, clipping a stroked (open)
        /// subject path produced a spurious straight line running from the subject down to
        /// the edge of the clipping rectangle, because the open path was passed through the
        /// polygon clipper which synthesised an edge out to the clip boundary.
        /// The document below contains a stroked path entirely inside the page/crop box.
        /// </summary>
        [Fact]
        public void StrokedPathInsideClipDoesNotProduceSpuriousLineToClipBoundary()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Halle12-1242"),
                new ParsingOptions { ClipPaths = true }))
            {
                var page = document.GetPage(1);

                // The page is clipped to its crop box. No extracted path may reach the bottom
                // edge of that box (y == 0) unless it is part of the clipping path itself.
                // Before the fix the open stroked path "leaked" a line down to y == 0.
                var spurious = page.Paths
                    .Select((path, index) => new { index, bounds = path.GetBoundingRectangle() })
                    .Where(x => x.bounds.HasValue && System.Math.Abs(x.bounds.Value.Bottom) < 0.001)
                    .ToList();

                Assert.Empty(spurious);
            }
        }
    }
}
