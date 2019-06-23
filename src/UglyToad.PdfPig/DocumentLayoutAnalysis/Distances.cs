using System;
using System.Collections.Generic;
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
        public static double WeightedEuclidean(PdfPoint point1, PdfPoint point2, double wX = 1.0, double wY = 1.0)
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
        public static double Manhattan(PdfPoint point1, PdfPoint point2)
        {
            return (double)(Math.Abs(point1.X - point2.X) + Math.Abs(point1.Y - point2.Y));
        }

        /// <summary>
        /// Find the nearest point.
        /// </summary>
        /// <param name="pdfPoint">The reference point, for which to find the nearest neighbour.</param>
        /// <param name="points">The list of neighbours candidates.</param>
        /// <param name="distanceMeasure">The distance measure to use.</param>
        /// <param name="distance">The distance between reference point, and its nearest neighbour</param>
        public static PdfPoint FindNearest(this PdfPoint pdfPoint, IReadOnlyList<PdfPoint> points,
            Func<PdfPoint, PdfPoint, double> distanceMeasure, out double distance)
        {
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException("Distances.FindNearest(): The list of neighbours candidates is either null or empty.", "points");
            }

            if (distanceMeasure == null)
            {
                throw new ArgumentException("Distances.FindNearest(): The distance measure must not be null.", "distanceMeasure");
            }
            
            distance = double.MaxValue;
            PdfPoint closestPoint = default;

            for (var i = 0; i < points.Count; i++)
            {
                double currentDistance = distanceMeasure(points[i], pdfPoint);
                if (currentDistance < distance)
                {
                    distance = currentDistance;
                    closestPoint = points[i];
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Find the index of the nearest point.
        /// </summary>
        /// <param name="pdfPoint">The reference point, for which to find the nearest neighbour.</param>
        /// <param name="points">The list of neighbours candidates.</param>
        /// <param name="distanceMeasure">The distance measure to use.</param>
        /// <param name="distance">The distance between reference point, and its nearest neighbour</param>
        public static int FindIndexNearest(this PdfPoint pdfPoint, IReadOnlyList<PdfPoint> points,
            Func<PdfPoint, PdfPoint, double> distanceMeasure, out double distance)
        {
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The list of neighbours candidates is either null or empty.", "points");
            }

            if (distanceMeasure == null)
            {
                throw new ArgumentException("Distances.FindIndexNearest(): The distance measure must not be null.", "distanceMeasure");
            }

            distance = double.MaxValue;
            int closestPointIndex = -1;

            for (var i = 0; i < points.Count; i++)
            {
                double currentDistance = distanceMeasure(points[i], pdfPoint);
                if (currentDistance < distance)
                {
                    distance = currentDistance;
                    closestPointIndex = i;
                }
            }

            return closestPointIndex;
        }
    }
}
