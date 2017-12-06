namespace UglyToad.Pdf.Tests.Geometry
{
    using Pdf.Geometry;
    using Xunit;

    public class PdfVectorTests
    {
        [Fact]
        public void ConstructorSetsValues()
        {
            var vector = new PdfVector(5.2m, 6.9m);

            Assert.Equal(5.2m, vector.X);
            Assert.Equal(6.9m, vector.Y);
        }

        [Fact]
        public void ScaleMultipliesLeavesOriginalUnchanged()
        {
            var vector = new PdfVector(5.2m, 6.9m);

            var scaled = vector.Scale(0.7m);

            Assert.Equal(5.2m, vector.X);
            Assert.Equal(5.2m * 0.7m, scaled.X);

            Assert.Equal(6.9m, vector.Y);
            Assert.Equal(6.9m * 0.7m, scaled.Y);
        }
    }
}
