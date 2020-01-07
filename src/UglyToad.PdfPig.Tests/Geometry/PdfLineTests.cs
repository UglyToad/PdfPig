namespace UglyToad.PdfPig.Tests.Geometry
{
    using System;
    using PdfPig.Core;
    using PdfPig.Geometry;
    using Xunit;

    public class PdfLineTests
    {
        [Fact]
        public void OriginIsZero()
        {
            var origin = new PdfLine();

            Assert.Equal(0, origin.Point1.X);
            Assert.Equal(0, origin.Point1.Y);
            Assert.Equal(0, origin.Point2.X);
            Assert.Equal(0, origin.Point2.Y);
        }

        [Fact]
        public void Length()
        {
            var line = new PdfLine(2, 1, 6, 4);
            Assert.Equal(5d, line.Length);

            var line2 = new PdfLine(-2, 8, -7, -5);
            Assert.Equal(13.93d, Math.Round(line2.Length, 2));
        }

        [Fact]
        public void Contains()
        {
            var line = new PdfLine(10, 7.5d, 26.3d, 12);
            Assert.False(line.Contains(new PdfPoint(5, 2)));
            Assert.False(line.Contains(new PdfPoint(5, 6.11963190184049d)));
            Assert.False(line.Contains(new PdfPoint(27, 12.1932515337423d)));
            Assert.False(line.Contains(new PdfPoint(12, 15)));
            Assert.False(line.Contains(new PdfPoint(10, 12)));
            Assert.True(line.Contains(new PdfPoint(20, 10.260736196319d)));
            Assert.True(line.Contains(new PdfPoint(10, 7.5d)));

            var verticalLine = new PdfLine(10, 7.5d, 10, 15);
            Assert.False(verticalLine.Contains(new PdfPoint(5, 2)));
            Assert.False(verticalLine.Contains(new PdfPoint(12, 15)));
            Assert.False(verticalLine.Contains(new PdfPoint(10, 16)));
            Assert.False(verticalLine.Contains(new PdfPoint(10, 7)));
            Assert.True(verticalLine.Contains(new PdfPoint(10, 12)));
            Assert.True(verticalLine.Contains(new PdfPoint(10, 7.5d)));

            var horizontalLine = new PdfLine(10, 7.5d, 26.3d, 7.5d);
            Assert.False(horizontalLine.Contains(new PdfPoint(5, 2)));
            Assert.False(horizontalLine.Contains(new PdfPoint(5, 7.5)));
            Assert.False(horizontalLine.Contains(new PdfPoint(27, 7.5)));
            Assert.False(horizontalLine.Contains(new PdfPoint(10, 12)));
            Assert.True(horizontalLine.Contains(new PdfPoint(20, 7.5)));
            Assert.True(horizontalLine.Contains(new PdfPoint(26.3d, 7.5d)));
        }

        [Fact]
        public void ParallelTo()
        {
            var verticalLine1 = new PdfLine(10, 7.5d, 10, 15);
            var verticalLine2 = new PdfLine(200, 0, 200, 551.5467d);
            var horizontalLine1 = new PdfLine(10, 7.5d, 26.3d, 7.5d);
            var horizontalLine2 = new PdfLine(27, 57, 200.9999872d, 57);
            var obliqueLine1 = new PdfLine(10, 7.5d, 26.3d, 12);
            var obliqueLine2 = new PdfLine(60, 28.8036809815951d, 40, 23.2822085889571d);

            Assert.True(verticalLine1.ParallelTo(verticalLine2));
            Assert.True(verticalLine2.ParallelTo(verticalLine1));

            Assert.False(obliqueLine1.ParallelTo(verticalLine2));
            Assert.False(verticalLine2.ParallelTo(obliqueLine1));

            Assert.False(obliqueLine1.ParallelTo(verticalLine1));
            Assert.False(verticalLine1.ParallelTo(obliqueLine1));

            Assert.True(horizontalLine1.ParallelTo(horizontalLine2));
            Assert.True(horizontalLine2.ParallelTo(horizontalLine1));

            Assert.False(obliqueLine1.ParallelTo(horizontalLine1));
            Assert.False(horizontalLine1.ParallelTo(obliqueLine1));

            Assert.False(obliqueLine1.ParallelTo(horizontalLine2));
            Assert.False(horizontalLine2.ParallelTo(obliqueLine1));

            Assert.False(verticalLine1.ParallelTo(horizontalLine2));
            Assert.False(horizontalLine2.ParallelTo(verticalLine1));

            Assert.False(verticalLine2.ParallelTo(horizontalLine2));
            Assert.False(horizontalLine2.ParallelTo(verticalLine2));

            Assert.True(obliqueLine1.ParallelTo(obliqueLine2));
            Assert.True(obliqueLine2.ParallelTo(obliqueLine1));
        }
    }
}
