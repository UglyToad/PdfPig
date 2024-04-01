﻿namespace UglyToad.PdfPig.Tests.Fonts
{
    using PdfPig.Core;
    using System.Text;

    public class CharacterPathTests
    {
        [Fact]
        public void BezierCurveGeneratesCorrectBoundingBox()
        {
            var curve = new PdfSubpath.CubicBezierCurve(new PdfPoint(60, 105),
                new PdfPoint(75, 30),
                new PdfPoint(215, 115),
                new PdfPoint(140, 160));

            var result = curve.GetBoundingRectangle();
            Assert.NotNull(result);
            Assert.Equal(160, result.Value.Top);
            // Extends beyond start but not as far as 1st control point.
            Assert.True(result.Value.Bottom < 105 && result.Value.Bottom > 30);
            // Extends beyond end but not as far as 2nd control point.
            Assert.True(result.Value.Right > 140 && result.Value.Right < 215);
            Assert.Equal(60, result.Value.Left);
        }

        [Fact]
        public void LoopBezierCurveGeneratesCorrectBoundingBox()
        {
            var curve = new PdfSubpath.CubicBezierCurve(new PdfPoint(166, 142),
                new PdfPoint(75, 30),
                new PdfPoint(215, 115),
                new PdfPoint(140, 160));
            
            var result = curve.GetBoundingRectangle();

            Assert.NotNull(result);
            Assert.Equal(160, result.Value.Top);
            // Extends beyond start but not as far as 1st control point.
            Assert.True(result.Value.Bottom < 142 && result.Value.Bottom > 30);
            Assert.Equal(166, result.Value.Right);
            // Extends beyond end.
            Assert.True(result.Value.Left < 140);
        }

        [Fact]
        public void BezierCurveAddsCorrectSvgCommand()
        {
            var curve = new PdfSubpath.CubicBezierCurve(new PdfPoint(60, 105),
                new PdfPoint(75, 30),
                new PdfPoint(215, 115),
                new PdfPoint(140, 160));

            var builder = new StringBuilder();
            curve.WriteSvg(builder, 0);

            Assert.Equal("C 75 -30, 215 -115, 140 -160 ", builder.ToString());
        }
    }
}
