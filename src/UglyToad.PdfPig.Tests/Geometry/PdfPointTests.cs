namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Core;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Geometry;
    using Xunit;

    public class PdfPointTests
    {
        #region data
        public static IEnumerable<object[]> GrahamScanData => new[]
        {
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(374.54011885, 950.71430641),
                    new PdfPoint(731.99394181, 598.6584842),
                    new PdfPoint(156.01864044, 155.99452034),
                    new PdfPoint(58.08361217, 866.17614577),
                    new PdfPoint(601.11501174, 708.0725778),
                    new PdfPoint(20.5844943, 969.90985216),
                    new PdfPoint(832.4426408, 212.33911068),
                    new PdfPoint(181.82496721, 183.40450985),
                    new PdfPoint(304.24224296, 524.75643163),
                    new PdfPoint(431.94501864, 291.2291402),
                    new PdfPoint(611.85289472, 139.49386065),
                    new PdfPoint(292.14464854, 366.36184329),
                    new PdfPoint(456.06998422, 785.17596139),
                    new PdfPoint(199.67378216, 514.23443841),
                    new PdfPoint(592.41456886, 46.45041272),
                    new PdfPoint(607.5448519, 170.52412369),
                    new PdfPoint(65.05159299, 948.88553725),
                    new PdfPoint(965.63203307, 808.39734812),
                    new PdfPoint(304.61376917, 97.67211401),
                    new PdfPoint(684.23302651, 440.15249374)
                },
                new PdfPoint[]
                {
                    new PdfPoint(374.54011885, 950.71430641),
                    new PdfPoint(156.01864044, 155.99452034),
                    new PdfPoint(20.5844943, 969.90985216),
                    new PdfPoint(832.4426408, 212.33911068),
                    new PdfPoint(592.41456886, 46.45041272),
                    new PdfPoint(965.63203307, 808.39734812),
                    new PdfPoint(304.61376917, 97.67211401)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(15.45661653, 928.31856259),
                    new PdfPoint(428.18414832, 966.65481904),
                    new PdfPoint(963.61997709, 853.00945547)
                },
                new PdfPoint[]
                {
                    new PdfPoint(15.45661653, 928.31856259),
                    new PdfPoint(428.18414832, 966.65481904),
                    new PdfPoint(963.61997709, 853.00945547)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(511.34239886, 501.51629469),
                    new PdfPoint(798.29517897, 649.96393078),
                    new PdfPoint(701.96687726, 795.79266944),
                    new PdfPoint(890.00534182, 337.99515685),
                    new PdfPoint(375.58295264, 93.98193984),
                    new PdfPoint(578.280141, 35.9422738),
                    new PdfPoint(465.59801813, 542.64463471),
                    new PdfPoint(286.54125213, 590.83326057),
                    new PdfPoint(30.50024994, 37.34818875),
                    new PdfPoint(822.60056066, 360.19064141)
                },
                new PdfPoint[]
                {
                    new PdfPoint(798.29517897, 649.96393078),
                    new PdfPoint(701.96687726, 795.79266944),
                    new PdfPoint(890.00534182, 337.99515685),
                    new PdfPoint(578.280141, 35.9422738),
                    new PdfPoint(286.54125213, 590.83326057),
                    new PdfPoint(30.50024994, 37.34818875)
                }
            }
        };
        #endregion

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

            Assert.Equal(0.534436, origin.X);
            Assert.Equal(0.32552, origin.Y);
        }

        [Theory]
        [MemberData(nameof(GrahamScanData))]
        public void GrahamScan(PdfPoint[] points, PdfPoint[] expected)
        {
            expected = expected.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            var convexHull = GeometryExtensions.GrahamScan(points).OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].X, convexHull[i].X, 6);
                Assert.Equal(expected[i].Y, convexHull[i].Y, 6);
            }
        }
    }
}
