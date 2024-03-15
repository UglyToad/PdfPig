namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Geometry;

    public class PdfVectorTests
    {
        [Fact]
        public void ConstructorSetsValues()
        {
            var vector = new PdfVector(5.2d, 6.9d);

            Assert.Equal(5.2d, vector.X);
            Assert.Equal(6.9d, vector.Y);
        }

        [Fact]
        public void ScaleMultipliesLeavesOriginalUnchanged()
        {
            var vector = new PdfVector(5.2d, 6.9d);

            var scaled = vector.Scale(0.7d);

            Assert.Equal(5.2d, vector.X);
            Assert.Equal(5.2d * 0.7d, scaled.X);

            Assert.Equal(6.9d, vector.Y);
            Assert.Equal(6.9d * 0.7d, scaled.Y);
        }
    }
}
