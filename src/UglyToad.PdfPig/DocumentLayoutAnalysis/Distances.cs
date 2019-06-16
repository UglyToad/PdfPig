using System;
using System.Linq;
using UglyToad.PdfPig.Geometry;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Contains helpful tools for distance measures.
    /// </summary>
    public static class Distances
    {
        /// <summary>
        /// The Euclidean distance is the "ordinary" straight-line distance between two points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <returns></returns>
        public static double Euclidean(PdfPoint point1, PdfPoint point2)
        {
            double dx = (double)(point1.X - point2.X);
            double dy = (double)(point1.Y - point2.Y);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// The weighted Euclidean distance.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <param name="wX">The weight of the X coordinates. Default is 1.</param>
        /// <param name="wY">The weight of the Y coordinates. Default is 1.</param>
        /// <returns></returns>
        public static double WghtdEuclidean(PdfPoint point1, PdfPoint point2, double wX = 1.0, double wY = 1.0)
        {
            double dx = (double)(point1.X - point2.X);
            double dy = (double)(point1.Y - point2.Y);
            return Math.Sqrt(wX * dx * dx + wY * dy * dy);
        }

        /// <summary>
        /// The Manhattan distance between two points is the sum of the absolute differences of their Cartesian coordinates.
        /// <para>Also known as rectilinear distance, L1 distance, L1 norm, snake distance, city block distance, taxicab metric.</para>
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <returns></returns>
        public static double Manhattan(PdfPoint point1, PdfPoint point2)
        {
            return (double)(Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y));
        }

        /// <summary>
        /// Find the nearest point.
        /// </summary>
        /// <param name="pdfPoint">The reference point, for which to find the nearest neighbour.</param>
        /// <param name="points">The list of neighbours candidates.</param>
        /// <param name="measure">The distance measure to use.</param>
        /// <param name="dist">The distance between reference point, and its nearest neighbour</param>
        /// <returns></returns>
        public static PdfPoint FindNearest(this PdfPoint pdfPoint, PdfPoint[] points,
            Func<PdfPoint, PdfPoint, double> measure, out double dist)
        {
            double d = points.Min(k => measure(k, pdfPoint));
            PdfPoint point = points.First(x => measure(x, pdfPoint) == d);
            dist = d;
            return point;
        }

        /// <summary>
        /// Find the index of the nearest point.
        /// </summary>
        /// <param name="pdfPoint">The reference point, for which to find the nearest neighbour.</param>
        /// <param name="points">The list of neighbours candidates.</param>
        /// <param name="measure">The distance measure to use.</param>
        /// <param name="dist">The distance between reference point, and its nearest neighbour</param>
        /// <returns></returns>
        public static int FindIndexNearest(this PdfPoint pdfPoint, PdfPoint[] points,
            Func<PdfPoint, PdfPoint, double> measure, out double dist)
        {
            double d = points.Min(k => measure(k, pdfPoint));
            int index = Array.FindIndex(points, x => measure(x, pdfPoint) == d);
            dist = d;
            return index;
        }
    }
}
