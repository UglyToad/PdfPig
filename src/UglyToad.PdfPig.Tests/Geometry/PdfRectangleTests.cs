using UglyToad.PdfPig.Geometry;
using Xunit;

namespace UglyToad.PdfPig.Tests.Geometry
{
    public class PdfRectangleTests
    {
        public void Area()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(100m, rectangle.Area);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376m, 687.13456m, 451.73539m, 1478.4997m);
            Assert.Equal(238819.4618743782m, rectangle1.Area);
        }

        public void Centroid()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(new PdfPoint(15, 15), rectangle.Centroid);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376m, 687.13456m, 451.73539m, 1478.4997m);
            Assert.Equal(new PdfPoint(300.844575m, 1082.81713m), rectangle1.Centroid);
        }

        public void Intersect()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376m, 687.13456m, 451.73539m, 1478.4997m);
            Assert.Null(rectangle.Intersect(rectangle1));
            Assert.Equal(rectangle1, rectangle1.Intersect(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456m, 350, 1478.4997m);
            Assert.Equal(new PdfRectangle(149.95376m, 687.13456m, 350, 1478.4997m), rectangle1.Intersect(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.Equal(rectangle3, rectangle1.Intersect(rectangle3));
        }

        public void IntersectsWith()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376m, 687.13456m, 451.73539m, 1478.4997m);
            Assert.False(rectangle.IntersectsWith(rectangle1));
            Assert.True(rectangle1.IntersectsWith(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456m, 350, 1478.4997m);
            Assert.True(rectangle1.IntersectsWith(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.True(rectangle1.IntersectsWith(rectangle3));

            PdfRectangle rectangle4 = new PdfRectangle(5, 7, 10, 25);
            Assert.False(rectangle1.IntersectsWith(rectangle4)); // special case where they share one border
        }

        public void Contains()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.True(rectangle.Contains(new PdfPoint(15, 15)));
            Assert.False(rectangle.Contains(new PdfPoint(10, 15)));
            Assert.True(rectangle.Contains(new PdfPoint(10, 15), true));
            Assert.False(rectangle.Contains(new PdfPoint(100, 100), true));
        }
    }
}
