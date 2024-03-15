﻿namespace UglyToad.PdfPig.Tests.Fonts.Type1
{
    using UglyToad.PdfPig.Core;
    using Integration;

    public class Type1CharStringParserTests
    {
        [Fact]
        public void CorrectBoundingBoxesFlexPoints()
        {
            var pointComparer = new PointComparer(new DoubleComparer(3));

            var filePath = IntegrationHelpers.GetDocumentPath("data.pdf");

            using var doc = PdfDocument.Open(filePath);
            var page = doc.GetPage(1);

            var letters = page.Letters;

            // check 'm'
            var m = letters[0];
            Assert.Equal("m", m.Value);
            Assert.Equal(new PdfPoint(253.4458, 658.431), m.GlyphRectangle.BottomLeft, pointComparer);
            Assert.Equal(new PdfPoint(261.22659, 662.83446), m.GlyphRectangle.TopRight, pointComparer);

            // check 'p'
            var p = letters[1];
            Assert.Equal("p", p.Value);
            Assert.Equal(new PdfPoint(261.70778, 656.49825), p.GlyphRectangle.BottomLeft, pointComparer);
            Assert.Equal(new PdfPoint(266.6193, 662.83446), p.GlyphRectangle.TopRight, pointComparer);
        }
    }
}
