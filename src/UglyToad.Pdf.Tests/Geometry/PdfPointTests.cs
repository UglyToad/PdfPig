namespace UglyToad.Pdf.Tests.Geometry
{
    using Pdf.Geometry;
    using Xunit;

    public class PdfPointTests
    {
        [Fact]
        public void OriginIsZero()
        {
            var origin = PdfPoint.Origin;

            Assert.Equal(0, origin.X);
            Assert.Equal(0, origin.Y);
        }

        [Fact]
        public void IntsSetValue()
        {
            var origin = new PdfPoint(256, 372);

            Assert.Equal(256, origin.X);
            Assert.Equal(372, origin.Y);
        }

        [Fact]
        public void DoublesSetValue()
        {
            var origin = new PdfPoint(0.534436, 0.32552);

            Assert.Equal(0.534436m, origin.X);
            Assert.Equal(0.32552m, origin.Y);
        }
    }
}
