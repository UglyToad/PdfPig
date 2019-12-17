using System;
using UglyToad.PdfPig.Geometry;
using Xunit;

namespace UglyToad.PdfPig.Tests.Geometry
{
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
            Assert.Equal(5m, line.Length);

            var line2 = new PdfLine(-2, 8, -7, -5);
            Assert.Equal(13.93m, Math.Round(line2.Length, 2));
        }

        [Fact]
        public void Contains()
        {
            var line = new PdfLine(10, 7.5m, 26.3m, 12);
            Assert.False(line.Contains(new PdfPoint(5, 2)));
            Assert.False(line.Contains(new PdfPoint(5, 6.11963190184049m)));
            Assert.False(line.Contains(new PdfPoint(27, 12.1932515337423m)));
            Assert.False(line.Contains(new PdfPoint(12, 15)));
            Assert.False(line.Contains(new PdfPoint(10, 12)));
            Assert.True(line.Contains(new PdfPoint(20, 10.260736196319m)));
            Assert.True(line.Contains(new PdfPoint(10, 7.5m)));

            var verticalLine = new PdfLine(10, 7.5m, 10, 15);
            Assert.False(verticalLine.Contains(new PdfPoint(5, 2)));
            Assert.False(verticalLine.Contains(new PdfPoint(12, 15)));
            Assert.False(verticalLine.Contains(new PdfPoint(10, 16)));
            Assert.False(verticalLine.Contains(new PdfPoint(10, 7)));
            Assert.True(verticalLine.Contains(new PdfPoint(10, 12)));
            Assert.True(verticalLine.Contains(new PdfPoint(10, 7.5m)));

            var horizontalLine = new PdfLine(10, 7.5m, 26.3m, 7.5m);
            Assert.False(horizontalLine.Contains(new PdfPoint(5, 2)));
            Assert.False(horizontalLine.Contains(new PdfPoint(5, 7.5)));
            Assert.False(horizontalLine.Contains(new PdfPoint(27, 7.5)));
            Assert.False(horizontalLine.Contains(new PdfPoint(10, 12)));
            Assert.True(horizontalLine.Contains(new PdfPoint(20, 7.5)));
            Assert.True(horizontalLine.Contains(new PdfPoint(26.3m, 7.5m)));
        }

        [Fact]
        public void ParallelTo()
        {
            var verticalLine1 = new PdfLine(10, 7.5m, 10, 15);
            var verticalLine2 = new PdfLine(200, 0, 200, 551.5467m);
            var horizontalLine1 = new PdfLine(10, 7.5m, 26.3m, 7.5m);
            var horizontalLine2 = new PdfLine(27, 57, 200.9999872m, 57);
            var obliqueLine1 = new PdfLine(10, 7.5m, 26.3m, 12);
            var obliqueLine2 = new PdfLine(60, 28.8036809815951m, 40, 23.2822085889571m);

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

        [Fact]
        public void IntersectsWithLine()
        {
            var verticalLine1 = new PdfLine(10, 7.5m, 10, 15);
            var verticalLine2 = new PdfLine(200, 0, 200, 551.5467m);
            var horizontalLine1 = new PdfLine(10, 7.5m, 26.3m, 7.5m);
            var horizontalLine2 = new PdfLine(27, 57, 200.9999872m, 57);
            var horizontalLine3 = new PdfLine(27, 57, 250, 57);
            var obliqueLine1 = new PdfLine(10, 7.5m, 26.3m, 12);
            var obliqueLine2 = new PdfLine(60, 28.8036809815951m, 40, 23.2822085889571m);
            var obliqueLine3 = new PdfLine(20, 7.5m, 10, 15);

            Assert.False(verticalLine1.IntersectsWith(verticalLine2));
            Assert.False(verticalLine2.IntersectsWith(verticalLine1));
            Assert.False(horizontalLine1.IntersectsWith(horizontalLine2));
            Assert.False(horizontalLine2.IntersectsWith(horizontalLine1));
            Assert.False(obliqueLine1.IntersectsWith(obliqueLine2));
            Assert.False(obliqueLine2.IntersectsWith(obliqueLine1));
            Assert.False(obliqueLine1.IntersectsWith(obliqueLine1));
            Assert.False(obliqueLine1.IntersectsWith(verticalLine2));
            Assert.False(verticalLine2.IntersectsWith(obliqueLine1));
            Assert.False(obliqueLine1.IntersectsWith(horizontalLine2));
            Assert.False(horizontalLine2.IntersectsWith(obliqueLine1));
            Assert.False(verticalLine1.IntersectsWith(horizontalLine2));
            Assert.False(horizontalLine2.IntersectsWith(verticalLine1));

            Assert.True(obliqueLine1.IntersectsWith(horizontalLine1));
            Assert.True(horizontalLine1.IntersectsWith(obliqueLine1));
            Assert.True(obliqueLine1.IntersectsWith(verticalLine1));
            Assert.True(verticalLine1.IntersectsWith(obliqueLine1));
            Assert.True(verticalLine2.IntersectsWith(horizontalLine2));
            Assert.True(horizontalLine2.IntersectsWith(verticalLine2));
            Assert.True(verticalLine2.IntersectsWith(horizontalLine3));
            Assert.True(horizontalLine3.IntersectsWith(verticalLine2));
            Assert.True(obliqueLine1.IntersectsWith(obliqueLine3));
            Assert.True(obliqueLine3.IntersectsWith(obliqueLine1));
        }

        [Fact]
        public void IntersectLine()
        {
            var verticalLine1 = new PdfLine(10, 7.5m, 10, 15);
            var verticalLine2 = new PdfLine(200, 0, 200, 551.5467m);
            var horizontalLine1 = new PdfLine(10, 7.5m, 26.3m, 7.5m);
            var horizontalLine2 = new PdfLine(27, 57, 200.9999872m, 57);
            var horizontalLine3 = new PdfLine(27, 57, 250, 57);
            var obliqueLine1 = new PdfLine(10, 7.5m, 26.3m, 12);
            var obliqueLine2 = new PdfLine(60, 28.8036809815951m, 40, 23.2822085889571m);
            var obliqueLine3 = new PdfLine(20, 7.5m, 10, 15);

            Assert.Null(verticalLine1.Intersect(verticalLine2));
            Assert.Null(verticalLine2.Intersect(verticalLine1));
            Assert.Null(horizontalLine1.Intersect(horizontalLine2));
            Assert.Null(horizontalLine2.Intersect(horizontalLine1));
            Assert.Null(obliqueLine1.Intersect(obliqueLine2));
            Assert.Null(obliqueLine2.Intersect(obliqueLine1));
            Assert.Null(obliqueLine1.Intersect(obliqueLine1));
            Assert.Null(obliqueLine1.Intersect(verticalLine2));
            Assert.Null(verticalLine2.Intersect(obliqueLine1));
            Assert.Null(obliqueLine1.Intersect(horizontalLine2));
            Assert.Null(horizontalLine2.Intersect(obliqueLine1));
            Assert.Null(verticalLine1.Intersect(horizontalLine2));
            Assert.Null(horizontalLine2.Intersect(verticalLine1));

            Assert.Equal(new PdfPoint(10, 7.5m), obliqueLine1.Intersect(horizontalLine1));
            Assert.Equal(new PdfPoint(10, 7.5m), horizontalLine1.Intersect(obliqueLine1));
            Assert.Equal(new PdfPoint(10, 7.5m), obliqueLine1.Intersect(verticalLine1));
            Assert.Equal(new PdfPoint(10, 7.5m), verticalLine1.Intersect(obliqueLine1));
            Assert.Equal(new PdfPoint(200, 57), verticalLine2.Intersect(horizontalLine2));
            Assert.Equal(new PdfPoint(200, 57), horizontalLine2.Intersect(verticalLine2));
            Assert.Equal(new PdfPoint(200, 57), verticalLine2.Intersect(horizontalLine3));
            Assert.Equal(new PdfPoint(200, 57), horizontalLine3.Intersect(verticalLine2));
            Assert.Equal(new PdfPoint(17.3094170403587m, 9.51793721973094m), obliqueLine1.Intersect(obliqueLine3));
            Assert.Equal(new PdfPoint(17.3094170403587m, 9.51793721973094m), obliqueLine3.Intersect(obliqueLine1));
        }
    }
}
