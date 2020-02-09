using UglyToad.PdfPig.Content;
using Xunit;

namespace UglyToad.PdfPig.Tests.Integration
{
    public class MarkedContentExtractionTests
    {
        private const string FileName1 = "Multiple Page - from Mortality Statistics.pdf";
        private const string FileName2 = "68-1990-01_A.pdf";

        [Fact]
        public void CanIncrementIndex()
        {
            using (var document = PdfDocument.Open(GetPath1()))
            {
                var page = document.GetPage(2);
                var mcs = page.GetMarkedContents();

                Assert.NotEmpty(mcs);

                Assert.Equal(37, mcs.Count);

                for (int i = 0; i < mcs.Count; i++)
                {
                    Assert.Equal(i, mcs[i].Index);
                }
            }
        }

        [Fact]
        public void CanGetTree()
        {
            using (var document = PdfDocument.Open(GetPath2()))
            {
                var page = document.GetPage(10);
                var mcs = page.GetMarkedContents();

                Assert.NotEmpty(mcs);
                Assert.Equal(86, mcs.Count);

                int index = 8;
                var mc = mcs[index];
                Assert.Equal(1, mc.Children.Count);
                Assert.Equal(index, mc.Index);
                Assert.NotEmpty(mc.Children);
                Assert.Equal(index, mc.Children[0].Index);
                Assert.DoesNotContain(mc.Children[0], mcs);

                index = 9;
                mc = mcs[index];
                Assert.Equal(1, mc.Children.Count);
                Assert.Equal(index, mc.Index);
                Assert.NotEmpty(mc.Children);
                Assert.Equal(index, mc.Children[0].Index);
                Assert.DoesNotContain(mc.Children[0], mcs);

                index = 75;
                mc = mcs[index];
                Assert.Equal(1, mc.Children.Count);
                Assert.Equal(index, mc.Index);
                Assert.NotEmpty(mc.Children);
                Assert.Equal(index, mc.Children[0].Index);
                Assert.DoesNotContain(mc.Children[0], mcs);
            }
        }

        [Fact]
        public void CanGetArtifact()
        {
            using (var document = PdfDocument.Open(GetPath1()))
            {
                var page = document.GetPage(2);
                var mcs = page.GetMarkedContents();

                Assert.NotEmpty(mcs);

                var content = mcs[0];
                Assert.True(content.IsArtifact);
                Assert.Equal(typeof(ArtifactMarkedContentElement), content.GetType());
                var artifact = (ArtifactMarkedContentElement)mcs[0];

                Assert.Equal(-1, artifact.MarkedContentIdentifier);

                Assert.True(artifact.IsTopAttached);
                Assert.False(artifact.IsRightAttached);
                Assert.False(artifact.IsLeftAttached);
                Assert.False(artifact.IsBottomAttached);

                Assert.True(artifact.BoundingBox.HasValue);
                Assert.Equal(89.03, artifact.BoundingBox.Value.BottomLeft.X);
                Assert.Equal(717.756, artifact.BoundingBox.Value.BottomLeft.Y);
                Assert.Equal(574.422, artifact.BoundingBox.Value.TopRight.X);
                Assert.Equal(751.1398, artifact.BoundingBox.Value.TopRight.Y);

                Assert.Equal(ArtifactMarkedContentElement.ArtifactType.Pagination, artifact.Type);
                Assert.Equal("Header", artifact.SubType);

                Assert.Equal(33, artifact.Letters.Count);
                Assert.Equal(8, artifact.Paths.Count);
                Assert.Equal(0, artifact.Images.Count);
            }
        }

        private static string GetPath1() => IntegrationHelpers.GetDocumentPath(FileName1);
        private static string GetPath2() => IntegrationHelpers.GetDocumentPath(FileName2);
    }
}
