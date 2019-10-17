namespace UglyToad.PdfPig.Tests.Fonts.CidFonts
{
    using System.Collections.Generic;
    using PdfPig.Fonts.CidFonts;
    using PdfPig.Geometry;
    using Xunit;

    public class VerticalWritingMetricsTests
    {
        private readonly VerticalVectorComponents defaults = new VerticalVectorComponents(250, 600);

        [Fact]
        public void UsesDefaultWhenOverridesNull()
        {
            var data = new VerticalWritingMetrics(defaults, null, null);

            Assert.Empty(data.IndividualVerticalWritingDisplacements);
            Assert.Empty(data.IndividualVerticalWritingPositions);

            var position = data.GetPositionVector(60, 250);
            Assert.Equal(defaults.Position, position.Y);

            var displacement = data.GetDisplacementVector(32);
            Assert.Equal(defaults.Displacement, displacement.Y);
        }

        [Fact]
        public void DefaultXComponentsOfVectorsAreCorrect()
        {
            var data = new VerticalWritingMetrics(defaults, null, null);

            var position = data.GetPositionVector(9, 120);
            Assert.Equal(120 / 2m, position.X);

            var displacement = data.GetDisplacementVector(10);
            Assert.Equal(0m, displacement.X);
        }

        [Fact]
        public void UsesVectorOverridesWhenPresent()
        {
            var data = new VerticalWritingMetrics(defaults, new Dictionary<int, decimal> {{7, 120}},
                new Dictionary<int, PdfVector> {{7, new PdfVector(25, 250)}});

            var position = data.GetPositionVector(7, 360);
            Assert.Equal(25, position.X);
            Assert.Equal(250, position.Y);

            var displacement = data.GetDisplacementVector(7);
            Assert.Equal(0, displacement.X);
            Assert.Equal(120, displacement.Y);

            var defaultPosition = data.GetPositionVector(6, 100);
            Assert.Equal(50, defaultPosition.X);
            Assert.Equal(defaults.Position, defaultPosition.Y);

            var defaultDisplacement = data.GetDisplacementVector(6);
            Assert.Equal(0, defaultDisplacement.X);
            Assert.Equal(defaults.Displacement, defaultDisplacement.Y);
        }
    }
}
