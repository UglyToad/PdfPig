namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Geometry;
    using PdfPig.Core;
    using Xunit;

    public class PdfRectangleTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(3);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(6);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);
        private static readonly PdfRectangle UnitRectangle = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1));

        [Fact]
        public void Area()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(100d, rectangle.Area);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(238819.4618743782d, rectangle1.Area, DoubleComparer);
        }

        [Fact]
        public void Centroid()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(new PdfPoint(15, 15), rectangle.Centroid);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(new PdfPoint(300.844575d, 1082.81713d), rectangle1.Centroid,
                PointComparer);
        }

        [Fact]
        public void Intersect()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Null(rectangle.Intersect(rectangle1));
            Assert.Equal(rectangle1, rectangle1.Intersect(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456d, 350, 1478.4997d);
            Assert.Equal(new PdfRectangle(149.95376d, 687.13456d, 350, 1478.4997d), rectangle1.Intersect(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.Equal(rectangle3, rectangle1.Intersect(rectangle3));
        }

        [Fact]
        public void IntersectsWith()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.False(rectangle.IntersectsWith(rectangle1));
            Assert.True(rectangle1.IntersectsWith(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456d, 350, 1478.4997d);
            Assert.True(rectangle1.IntersectsWith(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.True(rectangle1.IntersectsWith(rectangle3));

            PdfRectangle rectangle4 = new PdfRectangle(5, 7, 10, 25);
            Assert.False(rectangle1.IntersectsWith(rectangle4)); // special case where they share one border
        }

        [Fact]
        public void Contains()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.True(rectangle.Contains(new PdfPoint(15, 15)));
            Assert.False(rectangle.Contains(new PdfPoint(10, 15)));
            Assert.True(rectangle.Contains(new PdfPoint(10, 15), true));
            Assert.False(rectangle.Contains(new PdfPoint(100, 100), true));
        }

        [Fact]
        public void Translate()
        {
            var tm = TransformationMatrix.GetTranslationMatrix(5, 7);

            var translated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(5, 7), translated.BottomLeft);
            Assert.Equal(new PdfPoint(6, 7), translated.BottomRight);
            Assert.Equal(new PdfPoint(5, 8), translated.TopLeft);
            Assert.Equal(new PdfPoint(6, 8), translated.TopRight);

            Assert.Equal(1, translated.Width);
            Assert.Equal(1, translated.Height);
        }

        [Fact]
        public void Scale()
        {
            var tm = TransformationMatrix.GetScaleMatrix(3, 5);

            var scaled = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), scaled.BottomLeft);
            Assert.Equal(new PdfPoint(3, 0), scaled.BottomRight);
            Assert.Equal(new PdfPoint(0, 5), scaled.TopLeft);
            Assert.Equal(new PdfPoint(3, 5), scaled.TopRight);

            Assert.Equal(3, scaled.Width);
            Assert.Equal(5, scaled.Height);
        }

        [Fact]
        public void Rotate360()
        {
            var tm = TransformationMatrix.GetRotationMatrix(360);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(1, 0), rotated.BottomRight);
            Assert.Equal(new PdfPoint(0, 1), rotated.TopLeft);
            Assert.Equal(new PdfPoint(1, 1), rotated.TopRight);

            Assert.Equal(1, rotated.Width);
            Assert.Equal(1, rotated.Height);
        }

        [Fact]
        public void Rotate90()
        {
            var tm = TransformationMatrix.GetRotationMatrix(90);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(0, 1), rotated.BottomRight);
            Assert.Equal(new PdfPoint(-1, 0), rotated.TopLeft);
            Assert.Equal(new PdfPoint(-1, 1), rotated.TopRight);

            Assert.Equal(1, rotated.Width, PreciseDoubleComparer);
            Assert.Equal(-1, rotated.Height, PreciseDoubleComparer);
            Assert.Equal(90, rotated.Rotation, PreciseDoubleComparer);
        }

        [Fact]
        public void Rotate180()
        {
            var tm = TransformationMatrix.GetRotationMatrix(180);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(-1, 0), rotated.BottomRight);
            Assert.Equal(new PdfPoint(0, -1), rotated.TopLeft);
            Assert.Equal(new PdfPoint(-1, -1), rotated.TopRight);

            Assert.Equal(-1, rotated.Width, PreciseDoubleComparer);
            Assert.Equal(-1, rotated.Height, PreciseDoubleComparer);
            Assert.Equal(180, rotated.Rotation, PreciseDoubleComparer);
        }
    }
}
