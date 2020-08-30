namespace UglyToad.PdfPig.Geometry
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Geometry.ClipperLibrary;
    using UglyToad.PdfPig.Graphics;
    using static UglyToad.PdfPig.Core.PdfSubpath;

    /// <summary>
    /// Extension class to Geometry.
    /// </summary>
    public static class GeometryExtensions
    {
        private const double epsilon = 1e-5;

        /// <summary>
        /// Return true if the points are in counter-clockwise order.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        /// <param name="point3">The third point.</param>
        private static bool ccw(PdfPoint point1, PdfPoint point2, PdfPoint point3)
        {
            return (point2.X - point1.X) * (point3.Y - point1.Y) > (point2.Y - point1.Y) * (point3.X - point1.X);
        }

        #region PdfPoint
        /// <summary>
        /// Get the dot product of both points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static double DotProduct(this PdfPoint point1, PdfPoint point2)
        {
            return point1.X * point2.X + point1.Y * point2.Y;
        }

        /// <summary>
        /// Get a point with the summed coordinates of both points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static PdfPoint Add(this PdfPoint point1, PdfPoint point2)
        {
            return new PdfPoint(point1.X + point2.X, point1.Y + point2.Y);
        }

        /// <summary>
        /// Get a point with the substracted coordinates of both points.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public static PdfPoint Subtract(this PdfPoint point1, PdfPoint point2)
        {
            return new PdfPoint(point1.X - point2.X, point1.Y - point2.Y);
        }

        /// <summary>
        /// Algorithm to find a minimal bounding rectangle (MBR) such that the MBR corresponds to a rectangle 
        /// with smallest possible area completely enclosing the polygon.
        /// <para>From 'A Fast Algorithm for Generating a Minimal Bounding Rectangle' by Lennert D. Den Boer.</para>
        /// </summary>
        /// <param name="polygon">
        /// Polygon P is assumed to be both simple and convex, and to contain no duplicate (coincident) vertices.
        /// The vertices of P are assumed to be in strict cyclic sequential order, either clockwise or 
        /// counter-clockwise relative to the origin P0. 
        /// </param>
        private static PdfRectangle ParametricPerpendicularProjection(IReadOnlyList<PdfPoint> polygon)
        {
            if (polygon == null || polygon.Count == 0)
            {
                throw new ArgumentException("ParametricPerpendicularProjection(): polygon cannot be null and must contain at least one point.", nameof(polygon));
            }
            else if (polygon.Count == 1)
            {
                return new PdfRectangle(polygon[0], polygon[0]);
            }
            else if (polygon.Count == 2)
            {
                return new PdfRectangle(polygon[0], polygon[1]);
            }

            double[] MBR = new double[8];

            double Amin = double.PositiveInfinity;
            int j = 1;
            int k = 0;

            double QX = double.NaN;
            double QY = double.NaN;
            double R0X = double.NaN;
            double R0Y = double.NaN;
            double R1X = double.NaN;
            double R1Y = double.NaN;

            while (true)
            {
                PdfPoint Pk = polygon[k];
                PdfPoint Pj = polygon[j];

                double vX = Pj.X - Pk.X;
                double vY = Pj.Y - Pk.Y;
                double r = 1.0 / (vX * vX + vY * vY);

                double tmin = 1;
                double tmax = 0;
                double smax = 0;
                int l = -1;
                double uX;
                double uY;

                for (j = 0; j < polygon.Count; j++)
                {
                    Pj = polygon[j];
                    uX = Pj.X - Pk.X;
                    uY = Pj.Y - Pk.Y;
                    double t = (uX * vX + uY * vY) * r;

                    double PtX = t * vX + Pk.X;
                    double PtY = t * vY + Pk.Y;
                    uX = PtX - Pj.X;
                    uY = PtY - Pj.Y;

                    double s = uX * uX + uY * uY;

                    if (t < tmin)
                    {
                        tmin = t;
                        R0X = PtX;
                        R0Y = PtY;
                    }

                    if (t > tmax)
                    {
                        tmax = t;
                        R1X = PtX;
                        R1Y = PtY;
                    }

                    if (s > smax)
                    {
                        smax = s;
                        QX = PtX;
                        QY = PtY;
                        l = j;
                    }
                }

                if (l != -1)
                {
                    PdfPoint Pl = polygon[l];
                    double PlMinusQX = Pl.X - QX;
                    double PlMinusQY = Pl.Y - QY;

                    double R2X = R1X + PlMinusQX;
                    double R2Y = R1Y + PlMinusQY;

                    double R3X = R0X + PlMinusQX;
                    double R3Y = R0Y + PlMinusQY;

                    uX = R1X - R0X;
                    uY = R1Y - R0Y;

                    double A = (uX * uX + uY * uY) * smax;

                    if (A < Amin)
                    {
                        Amin = A;
                        MBR = new[] { R0X, R0Y, R1X, R1Y, R2X, R2Y, R3X, R3Y };
                    }
                }

                k++;
                j = k + 1;

                if (j == polygon.Count) j = 0;
                if (k == polygon.Count) break;
            }

            return new PdfRectangle(new PdfPoint(MBR[4], MBR[5]),
                                    new PdfPoint(MBR[6], MBR[7]),
                                    new PdfPoint(MBR[2], MBR[3]), 
                                    new PdfPoint(MBR[0], MBR[1]));
        }

        /// <summary>
        /// Algorithm to find the (oriented) minimum area rectangle (MAR) by first finding the convex hull of the points
        /// and then finding its MAR.
        /// </summary>
        /// <param name="points">The points.</param>
        public static PdfRectangle MinimumAreaRectangle(IEnumerable<PdfPoint> points)
        {
            if (points == null || points.Count() == 0)
            {
                throw new ArgumentException("MinimumAreaRectangle(): points cannot be null and must contain at least one point.", nameof(points));
            }

            return ParametricPerpendicularProjection(GrahamScan(points.Distinct()).ToList());
        }

        /// <summary>
        /// Algorithm to find the oriented bounding box (OBB) by first fitting a line through the points to get the slope,
        /// then rotating the points to obtain the axis-aligned bounding box (AABB), and then rotating back the AABB.
        /// </summary>
        /// <param name="points">The points.</param>
        public static PdfRectangle OrientedBoundingBox(IReadOnlyList<PdfPoint> points)
        {
            if (points == null || points.Count < 2)
            {
                throw new ArgumentException("OrientedBoundingBox(): points cannot be null and must contain at least two points.");
            }

            // Fitting a line through the points
            // to find the orientation (slope)
            double x0 = points.Average(p => p.X);
            double y0 = points.Average(p => p.Y);
            double sumProduct = 0;
            double sumDiffSquaredX = 0;

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var x_diff = point.X - x0;
                var y_diff = point.Y - y0;
                sumProduct += x_diff * y_diff;
                sumDiffSquaredX += x_diff * x_diff;
            }

            var slope = sumProduct / sumDiffSquaredX;

            // Rotate the points to build the axis-aligned bounding box (AABB)
            var angleRad = Math.Atan(slope); // -π/2 ≤ θ ≤ π/2
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            var inverseRotation = new TransformationMatrix(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1);

            var transformedPoints = points.Select(p => inverseRotation.Transform(p)).ToArray();
            var aabb = new PdfRectangle(transformedPoints.Min(p => p.X),
                                        transformedPoints.Min(p => p.Y),
                                        transformedPoints.Max(p => p.X),
                                        transformedPoints.Max(p => p.Y));

            // Rotate back the AABB to obtain to oriented bounding box (OBB)
            var rotateBack = new TransformationMatrix(
                cos, sin, 0,
                -sin, cos, 0,
                0, 0, 1);
            var obb = rotateBack.Transform(aabb);
            return obb;
        }

        /// <summary>
        /// Algorithm to find the convex hull of the set of points with time complexity O(n log n).
        /// </summary>
        public static IEnumerable<PdfPoint> GrahamScan(IEnumerable<PdfPoint> points)
        {
            if (points == null || points.Count() == 0)
            {
                throw new ArgumentException("GrahamScan(): points cannot be null and must contain at least one point.");
            }

            if (points.Count() < 3) return points;

            double polarAngle(PdfPoint point1, PdfPoint point2)
            {
                return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X) % Math.PI;
            }

            Stack<PdfPoint> stack = new Stack<PdfPoint>();
            var sortedPoints = points.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            var P0 = sortedPoints[0];
            var groups = sortedPoints.Skip(1).GroupBy(p => polarAngle(P0, p)).OrderBy(g => g.Key);

            sortedPoints = new List<PdfPoint>();
            foreach (var group in groups)
            {
                if (group.Count() == 1)
                {
                    sortedPoints.Add(group.First());
                }
                else
                {
                    // if more than one point has the same angle, 
                    // remove all but the one that is farthest from P0
                    sortedPoints.Add(group.OrderByDescending(p =>
                    {
                        double dx = p.X - P0.X;
                        double dy = p.Y - P0.Y;
                        return dx * dx + dy * dy;
                    }).First());
                }
            }

            if (sortedPoints.Count < 2)
            {
                return new[] { P0, sortedPoints[0] };
            }

            stack.Push(P0);
            stack.Push(sortedPoints[0]);
            stack.Push(sortedPoints[1]);

            for (int i = 2; i < sortedPoints.Count; i++)
            {
                var point = sortedPoints[i];
                while (!ccw(stack.ElementAt(1), stack.Peek(), point))
                {
                    stack.Pop();
                }
                stack.Push(point);
            }

            return stack;
        }
        #endregion

        #region PdfRectangle
        /// <summary>
        /// Whether the point is located inside the rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle that should contain the point.</param>
        /// <param name="point">The point that should be contained within the rectangle.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfRectangle rectangle, PdfPoint point, bool includeBorder = false)
        {
            if (Math.Abs(rectangle.Rotation) < epsilon)
            {
                if (includeBorder)
                {
                    return point.X >= rectangle.Left &&
                              point.X <= rectangle.Right &&
                              point.Y >= rectangle.Bottom &&
                              point.Y <= rectangle.Top;
                }

                return point.X > rectangle.Left &&
                       point.X < rectangle.Right &&
                       point.Y > rectangle.Bottom &&
                       point.Y < rectangle.Top;
            }
            else
            {
                double area(PdfPoint p1, PdfPoint p2, PdfPoint p3)
                {
                    return Math.Abs((p2.X * p1.Y - p1.X * p2.Y) + (p3.X * p2.Y - p2.X * p3.Y) + (p1.X * p3.Y - p3.X * p1.Y)) / 2.0;
                }

                var area1 = area(rectangle.BottomLeft, point, rectangle.TopLeft);
                var area2 = area(rectangle.TopLeft, point, rectangle.TopRight);
                var area3 = area(rectangle.TopRight, point, rectangle.BottomRight);
                var area4 = area(rectangle.BottomRight, point, rectangle.BottomLeft);

                var sum = area1 + area2 + area3 + area4; // sum is always greater or equal to area

                if (sum - rectangle.Area > epsilon) return false;

                if (area1 < epsilon || area2 < epsilon || area3 < epsilon || area4 < epsilon)
                {
                    // point is on the rectangle
                    return includeBorder;
                }

                return true;
            }
        }

        /// <summary>
        /// Whether the other rectangle is located inside the rectangle.
        /// </summary>
        /// <param name="rectangle">The rectangle that should contain the other rectangle.</param>
        /// <param name="other">The other rectangle that should be contained within the rectangle.</param>
        /// <param name="includeBorder">If set to false, will return false if the rectangles share side(s).</param>
        public static bool Contains(this PdfRectangle rectangle, PdfRectangle other, bool includeBorder = false)
        {
            if (!rectangle.Contains(other.BottomLeft, includeBorder)) return false;
            if (!rectangle.Contains(other.TopRight, includeBorder)) return false;
            if (!rectangle.Contains(other.BottomRight, includeBorder)) return false;
            if (!rectangle.Contains(other.TopLeft, includeBorder)) return false;

            return true;
        }

        /// <summary>
        /// Whether two rectangles overlap.
        /// <para>Returns false if the two rectangles only share a border.</para>
        /// </summary>
        public static bool IntersectsWith(this PdfRectangle rectangle, PdfRectangle other)
        {
            if (Math.Abs(rectangle.Rotation) < epsilon && Math.Abs(other.Rotation) < epsilon)
            {
                if (rectangle.Left > other.Right || other.Left > rectangle.Right)
                {
                    return false;
                }

                if (rectangle.Top < other.Bottom || other.Top < rectangle.Bottom)
                {
                    return false;
                }

                return true;
            }
            else
            {
                var r1 = rectangle.Normalise();
                var r2 = other.Normalise();
                if (Math.Abs(r1.Rotation) < epsilon && Math.Abs(r2.Rotation) < epsilon)
                {
                    // check rotation to avoid stackoverflow
                    if (!r1.IntersectsWith(r2))
                    {
                        return false;
                    }
                }

                if (rectangle.Contains(other.BottomLeft)) return true;
                if (rectangle.Contains(other.TopRight)) return true;
                if (rectangle.Contains(other.TopLeft)) return true;
                if (rectangle.Contains(other.BottomRight)) return true;

                if (other.Contains(rectangle.BottomLeft)) return true;
                if (other.Contains(rectangle.TopRight)) return true;
                if (other.Contains(rectangle.TopLeft)) return true;
                if (other.Contains(rectangle.BottomRight)) return true;

                if (IntersectsWith(rectangle.BottomLeft, rectangle.BottomRight, other.BottomLeft, other.BottomRight)) return true;
                if (IntersectsWith(rectangle.BottomLeft, rectangle.BottomRight, other.BottomRight, other.TopRight)) return true;
                if (IntersectsWith(rectangle.BottomLeft, rectangle.BottomRight, other.TopRight, other.TopLeft)) return true;
                if (IntersectsWith(rectangle.BottomLeft, rectangle.BottomRight,other.TopLeft, other.BottomLeft)) return true;
                
                if (IntersectsWith(rectangle.BottomRight, rectangle.TopRight, other.BottomLeft, other.BottomRight)) return true;
                if (IntersectsWith(rectangle.BottomRight, rectangle.TopRight,other.BottomRight, other.TopRight)) return true;
                if (IntersectsWith(rectangle.BottomRight, rectangle.TopRight, other.TopRight, other.TopLeft)) return true;
                if (IntersectsWith(rectangle.BottomRight, rectangle.TopRight, other.TopLeft, other.BottomLeft)) return true;
                
                if (IntersectsWith(rectangle.TopRight, rectangle.TopLeft, other.BottomLeft, other.BottomRight)) return true;
                if (IntersectsWith(rectangle.TopRight, rectangle.TopLeft, other.BottomRight, other.TopRight)) return true;
                if (IntersectsWith(rectangle.TopRight, rectangle.TopLeft, other.TopRight, other.TopLeft)) return true;
                if (IntersectsWith(rectangle.TopRight, rectangle.TopLeft, other.TopLeft, other.BottomLeft)) return true;

                if (IntersectsWith(rectangle.TopLeft, rectangle.BottomLeft, other.BottomLeft, other.BottomRight)) return true;
                if (IntersectsWith(rectangle.TopLeft, rectangle.BottomLeft, other.BottomRight, other.TopRight)) return true;
                if (IntersectsWith(rectangle.TopLeft, rectangle.BottomLeft, other.TopRight, other.TopLeft)) return true;
                if (IntersectsWith(rectangle.TopLeft, rectangle.BottomLeft, other.TopLeft, other.BottomLeft)) return true;

                return false;
            }
        }

        /// <summary>
        /// Gets the <see cref="PdfRectangle"/> that is the intersection of two rectangles.
        /// <para>Only works for axis-aligned rectangles.</para>
        /// </summary>
        public static PdfRectangle? Intersect(this PdfRectangle rectangle, PdfRectangle other)
        {
            if (!rectangle.IntersectsWith(other)) return null;
            return new PdfRectangle(Math.Max(rectangle.BottomLeft.X, other.BottomLeft.X),
                                    Math.Max(rectangle.BottomLeft.Y, other.BottomLeft.Y),
                                    Math.Min(rectangle.TopRight.X, other.TopRight.X),
                                    Math.Min(rectangle.TopRight.Y, other.TopRight.Y));
        }

        /// <summary>
        /// Gets the axis-aligned rectangle that completely containing the original rectangle, with no rotation.
        /// </summary>
        /// <param name="rectangle"></param>
        public static PdfRectangle Normalise(this PdfRectangle rectangle)
        {
            var points = new[] { rectangle.BottomLeft, rectangle.BottomRight, rectangle.TopLeft, rectangle.TopRight };
            return new PdfRectangle(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
        }
        #endregion

        #region PdfLine
        /// <summary>
        /// Whether the point is located on the line segment.
        /// </summary>
        public static bool Contains(this PdfLine line, PdfPoint point)
        {
            return Contains(line.Point1, line.Point2, point);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfLine line, PdfLine other)
        {
            return IntersectsWith(line.Point1, line.Point2, other.Point1, other.Point2);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfLine line, Line other)
        {
            return IntersectsWith(line.Point1, line.Point2, other.From, other.To);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfLine line, PdfLine other)
        {
            return Intersect(line.Point1, line.Point2, other.Point1, other.Point2);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfLine line, Line other)
        {
            return Intersect(line.Point1, line.Point2, other.From, other.To);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfLine line, PdfLine other)
        {
            return ParallelTo(line.Point1, line.Point2, other.Point1, other.Point2);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfLine line, Line other)
        {
            return ParallelTo(line.Point1, line.Point2, other.From, other.To);
        }
        #endregion

        #region Path Line
        /// <summary>
        /// Whether the point is located on the line segment.
        /// </summary>
        public static bool Contains(this Line line, PdfPoint point)
        {
            return Contains(line.From, line.To, point);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this Line line, Line other)
        {
            return IntersectsWith(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this Line line, PdfLine other)
        {
            return IntersectsWith(line.From, line.To, other.Point1, other.Point2);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this Line line, Line other)
        {
            return Intersect(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this Line line, PdfLine other)
        {
            return Intersect(line.From, line.To, other.Point1, other.Point2);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this Line line, Line other)
        {
            return ParallelTo(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this Line line, PdfLine other)
        {
            return ParallelTo(line.From, line.To, other.Point1, other.Point2);
        }
        #endregion

        #region Generic line
        private static bool Contains(PdfPoint pl1, PdfPoint pl2, PdfPoint point)
        {
            if (Math.Abs(pl2.X - pl1.X) < epsilon)
            {
                if (Math.Abs(point.X - pl2.X) < epsilon)
                {
                    return Math.Abs(Math.Sign(point.Y - pl2.Y) - Math.Sign(point.Y - pl1.Y)) > epsilon;
                }
                return false;
            }

            if (Math.Abs(pl2.Y - pl1.Y) < epsilon)
            {
                if (Math.Abs(point.Y - pl2.Y) < epsilon)
                {
                    return Math.Abs(Math.Sign(point.X - pl2.X) - Math.Sign(point.X - pl1.X)) > epsilon;
                }
                return false;
            }

            var tx = (point.X - pl1.X) / (pl2.X - pl1.X);
            var ty = (point.Y - pl1.Y) / (pl2.Y - pl1.Y);
            if (Math.Abs(tx - ty) > epsilon) return false;
            return tx >= 0 && (tx - 1) <= epsilon;
        }

        /// <summary>
        /// Whether the line formed by <paramref name="p11"/> and <paramref name="p12"/>
        /// intersects the line formed by <paramref name="p21"/> and <paramref name="p22"/>.
        /// </summary>
        public static bool IntersectsWith(PdfPoint p11, PdfPoint p12, PdfPoint p21, PdfPoint p22)
        {
            return (ccw(p11, p12, p21) != ccw(p11, p12, p22)) &&
                   (ccw(p21, p22, p11) != ccw(p21, p22, p12));
        }

        private static PdfPoint? Intersect(PdfPoint p11, PdfPoint p12, PdfPoint p21, PdfPoint p22)
        {
            if (!IntersectsWith(p11, p12, p21, p22)) return null;

            var (Slope1, Intercept1) = GetSlopeIntercept(p11, p12);
            var (Slope2, Intercept2) = GetSlopeIntercept(p21, p22);

            if (double.IsNaN(Slope1))
            {
                var x = Intercept1;
                var y = Slope2 * x + Intercept2;
                return new PdfPoint(x, y);
            }
            else if (double.IsNaN(Slope2))
            {
                var x = Intercept2;
                var y = Slope1 * x + Intercept1;
                return new PdfPoint(x, y);
            }
            else
            {
                var x = (Intercept2 - Intercept1) / (Slope1 - Slope2);
                var y = Slope1 * x + Intercept1;
                return new PdfPoint(x, y);
            }
        }

        private static bool ParallelTo(PdfPoint p11, PdfPoint p12, PdfPoint p21, PdfPoint p22)
        {
            return Math.Abs((p12.Y - p11.Y) * (p22.X - p21.X) - (p22.Y - p21.Y) * (p12.X - p11.X)) < epsilon;
        }
        #endregion

        #region Path Bezier Curve
        /// <summary>
        /// Split a bezier curve into 2 bezier curves, at tau.
        /// </summary>
        /// <param name="bezierCurve">The original bezier curve.</param>
        /// <param name="tau">The t value were to split the curve, usually between 0 and 1, but not necessary.</param>
        public static (BezierCurve, BezierCurve) Split(this BezierCurve bezierCurve, double tau)
        {
            // De Casteljau Algorithm
            PdfPoint[][] points = new PdfPoint[4][];

            points[0] = new[]
            {
                bezierCurve.StartPoint,
                bezierCurve.FirstControlPoint,
                bezierCurve.SecondControlPoint,
                bezierCurve.EndPoint
            };

            points[1] = new PdfPoint[3];
            points[2] = new PdfPoint[2];
            points[3] = new PdfPoint[1];

            for (int j = 1; j <= 3; j++)
            {
                for (int i = 0; i <= 3 - j; i++)
                {
                    var x = (1 - tau) * points[j - 1][i].X + tau * points[j - 1][i + 1].X;
                    var y = (1 - tau) * points[j - 1][i].Y + tau * points[j - 1][i + 1].Y;
                    points[j][i] = new PdfPoint(x, y);
                }
            }

            return (new BezierCurve(points[0][0], points[1][0], points[2][0], points[3][0]),
                    new BezierCurve(points[3][0], points[2][1], points[1][2], points[0][3]));
        }

        /// <summary>
        /// Checks if the curve and the line are intersecting.
        /// <para>Avoid using this method as it is not optimised. Use <see cref="Intersect(BezierCurve, PdfLine)"/> instead.</para>
        /// </summary>
        public static bool IntersectsWith(this BezierCurve bezierCurve, PdfLine line)
        {
            return IntersectsWith(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Checks if the curve and the line are intersecting.
        /// <para>Avoid using this method as it is not optimised. Use <see cref="Intersect(BezierCurve, Line)"/> instead.</para>
        /// </summary>
        public static bool IntersectsWith(this BezierCurve bezierCurve, Line line)
        {
            return IntersectsWith(bezierCurve, line.From, line.To);
        }

        private static bool IntersectsWith(BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
        {
            return Intersect(bezierCurve, p1, p2).Length > 0;
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        public static PdfPoint[] Intersect(this BezierCurve bezierCurve, PdfLine line)
        {
            return Intersect(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        public static PdfPoint[] Intersect(this BezierCurve bezierCurve, Line line)
        {
            return Intersect(bezierCurve, line.From, line.To);
        }
        
        private static PdfPoint[] Intersect(BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
        {
            var ts = IntersectT(bezierCurve, p1, p2);
            if (ts == null || ts.Length == 0) return EmptyArray<PdfPoint>.Instance;

            List<PdfPoint> points = new List<PdfPoint>();
            foreach (var t in ts)
            {
                PdfPoint point = new PdfPoint(
                    BezierCurve.ValueWithT(bezierCurve.StartPoint.X,
                                           bezierCurve.FirstControlPoint.X,
                                           bezierCurve.SecondControlPoint.X,
                                           bezierCurve.EndPoint.X,
                                           t),
                    BezierCurve.ValueWithT(bezierCurve.StartPoint.Y,
                                           bezierCurve.FirstControlPoint.Y,
                                           bezierCurve.SecondControlPoint.Y,
                                           bezierCurve.EndPoint.Y,
                                           t));
                if (Contains(p1, p2, point)) points.Add(point);
            }
            return points.ToArray();
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="BezierCurve"/> and the <see cref="PdfLine"/> intersect.</returns>
        public static double[] IntersectT(this BezierCurve bezierCurve, PdfLine line)
        {
            return IntersectT(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="BezierCurve"/> and the <see cref="Line"/> intersect.</returns>
        public static double[] IntersectT(this BezierCurve bezierCurve, Line line)
        {
            return IntersectT(bezierCurve, line.From, line.To);
        }

        private static double[] IntersectT(BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
        {
            // if the bounding boxes do not intersect, they cannot intersect
            var bezierBbox = bezierCurve.GetBoundingRectangle();
            if (!bezierBbox.HasValue) return null;

            if (bezierBbox.Value.Left > Math.Max(p1.X, p2.X) || Math.Min(p1.X, p2.X) > bezierBbox.Value.Right)
            {
                return null;
            }

            if (bezierBbox.Value.Top < Math.Min(p1.Y, p2.Y) || Math.Max(p1.Y, p2.Y) < bezierBbox.Value.Bottom)
            {
                return null;
            }

            double A = (p2.Y - p1.Y);
            double B = (p1.X - p2.X);
            double C = p1.X * (p1.Y - p2.Y) + p1.Y * (p2.X - p1.X);

            double alpha = bezierCurve.StartPoint.X * A + bezierCurve.StartPoint.Y * B;
            double beta = 3.0 * (bezierCurve.FirstControlPoint.X * A + bezierCurve.FirstControlPoint.Y * B);
            double gamma = 3.0 * (bezierCurve.SecondControlPoint.X * A + bezierCurve.SecondControlPoint.Y * B);
            double delta = bezierCurve.EndPoint.X * A + bezierCurve.EndPoint.Y * B;

            double a = -alpha + beta - gamma + delta;
            double b = 3 * alpha - 2 * beta + gamma;
            double c = -3 * alpha + beta;
            double d = alpha + C;

            var solution = SolveCubicEquation(a, b, c, d);

            return solution.Where(s => !double.IsNaN(s)).Where(s => s >= -epsilon && (s - 1) <= epsilon).OrderBy(s => s).ToArray();
        }
        #endregion

        #region PdfPath & PdfSubpath
        #region Clipper extension
        // https://stackoverflow.com/questions/54723622/point-in-polygon-hit-test-algorithm
        // Ported from Angus Johnson's Delphi Pascal code (Clipper's author)
        // Might be made available in the next Clipper release?

        private static double CrossProduct(ClipperIntPoint pt1, ClipperIntPoint pt2, ClipperIntPoint pt3)
        {
            return (pt2.X - pt1.X) * (pt3.Y - pt2.Y) - (pt2.Y - pt1.Y) * (pt3.X - pt2.X);
        }

        /// <summary>
        /// nb: returns MaxInt ((2^32)-1) when pt is on a line
        /// </summary>
        private static int PointInPathsWindingCount(ClipperIntPoint pt, List<List<ClipperIntPoint>> paths)
        {
            var result = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                int j = 0;
                List<ClipperIntPoint> p = paths[i];
                int len = p.Count;

                if (len < 3) continue;
                ClipperIntPoint prevPt = p[len - 1];

                while ((j < len) && (p[j].Y == prevPt.Y)) j++;
                if (j == len) continue;

                bool isAbove = prevPt.Y < pt.Y;

                while (j < len)
                {
                    if (isAbove)
                    {
                        while ((j < len) && (p[j].Y < pt.Y)) j++;
                        if (j == len)
                        {
                            break;
                        }
                        else if (j > 0)
                        {
                            prevPt = p[j - 1];
                        }

                        double crossProd = CrossProduct(prevPt, p[j], pt);
                        if (crossProd == 0)
                        {
                            return int.MaxValue;
                        }
                        else if (crossProd < 0)
                        {
                            result--;
                        }
                    }
                    else
                    {
                        while ((j < len) && (p[j].Y > pt.Y)) j++;
                        if (j == len)
                        {
                            break;
                        }
                        else if (j > 0)
                        {
                            prevPt = p[j - 1];
                        }

                        double crossProd = CrossProduct(prevPt, p[j], pt);
                        if (crossProd == 0)
                        {
                            return int.MaxValue;
                        }
                        else if (crossProd > 0)
                        {
                            result++;
                        }
                    }

                    j++;
                    isAbove = !isAbove;
                }
            }
            return result;
        }

        private static bool PointInPaths(ClipperIntPoint pt, List<List<ClipperIntPoint>> paths, ClipperPolyFillType fillRule, bool includeBorder)
        {
            int wc = PointInPathsWindingCount(pt, paths);
            if (wc == int.MaxValue)
            {
                return includeBorder;
            }

            switch (fillRule)
            {
                default:
                case ClipperPolyFillType.EvenOdd:
                    return wc % 2 != 0;

                case ClipperPolyFillType.NonZero:
                    return wc != 0;
            }
        }
        #endregion

        /// <summary>
        /// Whether the point is located inside the subpath.
        /// <para>Ignores winding rule.</para>
        /// </summary>
        /// <param name="subpath">The subpath that should contain the point.</param>
        /// <param name="point">The point that should be contained within the subpath.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfSubpath subpath, PdfPoint point, bool includeBorder = false)
        {
            return PointInPaths(point.ToClipperIntPoint(),
                new List<List<ClipperIntPoint>>() { subpath.ToClipperPolygon().ToList() },
                ClipperPolyFillType.EvenOdd,
                includeBorder);
        }

        /// <summary>
        /// Whether the rectangle is located inside the subpath.
        /// <para>Ignores winding rule.</para>
        /// </summary>
        /// <param name="subpath">The subpath that should contain the rectangle.</param>
        /// <param name="rectangle">The rectangle that should be contained within the subpath.</param>
        /// <param name="includeBorder">[Not used for the moment] If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfSubpath subpath, PdfRectangle rectangle, bool includeBorder = false)
        {
            var clipperPaths = new List<List<ClipperIntPoint>>() { subpath.ToClipperPolygon().ToList() };
            if (!PointInPaths(rectangle.BottomLeft.ToClipperIntPoint(), clipperPaths, ClipperPolyFillType.EvenOdd, includeBorder)) return false;
            if (!PointInPaths(rectangle.TopLeft.ToClipperIntPoint(), clipperPaths, ClipperPolyFillType.EvenOdd, includeBorder)) return false;
            if (!PointInPaths(rectangle.TopRight.ToClipperIntPoint(), clipperPaths, ClipperPolyFillType.EvenOdd, includeBorder)) return false;
            if (!PointInPaths(rectangle.BottomRight.ToClipperIntPoint(), clipperPaths, ClipperPolyFillType.EvenOdd, includeBorder)) return false;
            return true;
        }

        /// <summary>
        /// Whether the other subpath is located inside the subpath.
        /// <para>Ignores winding rule.</para>
        /// </summary>
        /// <param name="subpath">The subpath that should contain the rectangle.</param>
        /// <param name="other">The other subpath that should be contained within the subpath.</param>
        /// <param name="includeBorder">[Not used for the moment] If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfSubpath subpath, PdfSubpath other, bool includeBorder = false)
        {
            var clipperPaths = new List<List<ClipperIntPoint>>() { subpath.ToClipperPolygon().ToList() };
            foreach (var pt in other.ToClipperPolygon())
            {
                if (!PointInPaths(pt, clipperPaths, ClipperPolyFillType.EvenOdd, includeBorder)) return false;
            }
            return true;
        }

        /// <summary>
        /// Get the area of the path.
        /// </summary>
        /// <param name="path"></param>
        public static double GetArea(this PdfPath path)
        {
            var clipperPaths = path.Select(sp => sp.ToClipperPolygon().ToList()).ToList();
            var simplifieds = Clipper.SimplifyPolygons(clipperPaths, path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd);
            double sum = 0;
            foreach (var simplified in simplifieds)
            {
                sum += Clipper.Area(simplified);
            }
            return sum;
        }

        /// <summary>
        /// Whether the point is located inside the path.
        /// </summary>
        /// <param name="path">The path that should contain the point.</param>
        /// <param name="point">The point that should be contained within the path.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfPath path, PdfPoint point, bool includeBorder = false)
        {
            var clipperPaths = path.Select(sp => sp.ToClipperPolygon().ToList()).ToList();
            return PointInPaths(point.ToClipperIntPoint(),
                clipperPaths,
                path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd,
                includeBorder);
        }

        /// <summary>
        /// Whether the rectangle is located inside the path.
        /// </summary>
        /// <param name="path">The path that should contain the rectangle.</param>
        /// <param name="rectangle">The rectangle that should be contained within the path.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfPath path, PdfRectangle rectangle, bool includeBorder = false)
        {
            var clipperPaths = path.Select(sp => sp.ToClipperPolygon().ToList()).ToList();
            var fillType = path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;
            if (!PointInPaths(rectangle.BottomLeft.ToClipperIntPoint(), clipperPaths, fillType, includeBorder)) return false;
            if (!PointInPaths(rectangle.TopLeft.ToClipperIntPoint(), clipperPaths, fillType, includeBorder)) return false;
            if (!PointInPaths(rectangle.TopRight.ToClipperIntPoint(), clipperPaths, fillType, includeBorder)) return false;
            if (!PointInPaths(rectangle.BottomRight.ToClipperIntPoint(), clipperPaths, fillType, includeBorder)) return false;
            return true;
        }

        /// <summary>
        /// Whether the subpath is located inside the path.
        /// </summary>
        /// <param name="path">The path that should contain the subpath.</param>
        /// <param name="subpath">The subpath that should be contained within the path.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfPath path, PdfSubpath subpath, bool includeBorder = false)
        {
            var clipperPaths = path.Select(sp => sp.ToClipperPolygon().ToList()).ToList();
            var fillType = path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;
            foreach (var p in subpath.ToClipperPolygon())
            {
                if (!PointInPaths(p, clipperPaths, fillType, includeBorder)) return false;
            }
            return true;
        }

        /// <summary>
        /// Whether the other path is located inside the path.
        /// </summary>
        /// <param name="path">The path that should contain the path.</param>
        /// <param name="other">The other path that should be contained within the path.</param>
        /// <param name="includeBorder">If set to false, will return false if the point belongs to the border.</param>
        public static bool Contains(this PdfPath path, PdfPath other, bool includeBorder = false)
        {
            var clipperPaths = path.Select(sp => sp.ToClipperPolygon().ToList()).ToList();
            var fillType = path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;
            foreach (var subpath in other)
            {
                foreach (var p in subpath.ToClipperPolygon())
                {
                    if (!PointInPaths(p, clipperPaths, fillType, includeBorder)) return false;
                }
            }
            return true;
        }

        #endregion

        private const double OneThird = 0.333333333333333333333;
        private const double SqrtOfThree = 1.73205080756888;

        private static (double Slope, double Intercept) GetSlopeIntercept(PdfPoint point1, PdfPoint point2)
        {
            if (Math.Abs(point1.X - point2.X) > epsilon)
            {
                var slope = (point2.Y - point1.Y) / (point2.X - point1.X);
                var intercept = point2.Y - slope * point2.X;
                return (slope, intercept);
            }
            else
            {
                // vertical line special case
                return (double.NaN, point1.X);
            }
        }

        private static double CubicRoot(double d)
        {
            if (d < 0.0) return -Math.Pow(-d, OneThird);
            return Math.Pow(d, OneThird);
        }

        /// <summary>
        /// Get the real roots of a Cubic (or Quadratic, a=0) equation.
        /// <para>ax^3 + bx^2 + cx + d = 0</para>
        /// </summary>
        /// <param name="a">ax^3</param>
        /// <param name="b">bx^2</param>
        /// <param name="c">cx</param>
        /// <param name="d">d</param>
        private static double[] SolveCubicEquation(double a, double b, double c, double d)
        {
            if (Math.Abs(a) <= epsilon)
            {
                // handle Quadratic equation (a=0)
                double detQ = c * c - 4 * b * d;
                if (detQ >= 0)
                {
                    double sqrtDetQ = Math.Sqrt(detQ);
                    double OneOverTwiceB = 1 / (2.0 * b);
                    double x = (-c + sqrtDetQ) * OneOverTwiceB;
                    double x0 = (-c - sqrtDetQ) * OneOverTwiceB;
                    return new double[] { x, x0 };
                }
                return EmptyArray<double>.Instance; // no real roots
            }

            double aSquared = a * a;
            double aCubed = aSquared * a;
            double bCubed = b * b * b;
            double abc = a * b * c;
            double bOver3a = b / (3.0 * a);

            double Q = (3.0 * a * c - b * b) / (9.0 * aSquared);
            double R = (9.0 * abc - 27.0 * aSquared * d - 2.0 * bCubed) / (54.0 * aCubed);

            double det = Q * Q * Q + R * R;  // same sign as determinant because: 4p^3 + 27q^2 = (4 * 27) * (Q^3 + R^2)
            double x1 = double.NaN;
            double x2 = double.NaN;
            double x3 = double.NaN;

            if (det >= 0) // Cardano's Formula
            {
                double sqrtDet = Math.Sqrt(det);

                double S = CubicRoot(R + sqrtDet);
                double T = CubicRoot(R - sqrtDet);
                double SPlusT = S + T;

                x1 = SPlusT - bOver3a;           // real root

                // Complex roots
                double complexPart = SqrtOfThree / 2.0 * (S - T); // complex part of complex root
                if (Math.Abs(complexPart) <= epsilon) // if complex part == 0
                {
                    // complex roots only have real part
                    // the real part is the same for both roots
                    x2 = -SPlusT / 2 - bOver3a;
                }
            }
            else // Casus irreducibilis
            {
                // François Viète's formula
                double vietTrigonometricSolution(double p_, double q_, double k) => 2.0 * Math.Sqrt(-p_ / 3.0)
                        * Math.Cos(OneThird * Math.Acos((3.0 * q_) / (2.0 * p_) * Math.Sqrt(-3.0 / p_)) - (2.0 * Math.PI * k) / 3.0);

                double p = Q * 3.0;         // (3.0 * a * c - b * b) / (3.0 * aSquared);
                double q = -R * 2.0;        // (2.0 * bCubed - 9.0 * abc + 27.0 * aSquared * d) / (27.0 * aCubed);
                x1 = vietTrigonometricSolution(p, q, 0) - bOver3a;
                x2 = vietTrigonometricSolution(p, q, 1) - bOver3a;
                x3 = vietTrigonometricSolution(p, q, 2) - bOver3a;
            }

            return new[] {x1, x2, x3};
        }

        internal static string ToSvg(this PdfSubpath p, double height)
        {
            var builder = new StringBuilder();
            foreach (var pathCommand in p.Commands)
            {
                pathCommand.WriteSvg(builder, height);
            }

            if (builder.Length == 0)
            {
                return string.Empty;
            }

            if (builder[builder.Length - 1] == ' ')
            {
                builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        internal static string ToFullSvg(this PdfSubpath p, double height)
        {
            string BboxToRect(PdfRectangle box, string stroke)
            {
                var overallBbox = $"<rect x='{box.Left}' y='{box.Bottom}' width='{box.Width}' height='{box.Height}' stroke-width='2' fill='none' stroke='{stroke}'></rect>";
                return overallBbox;
            }

            var glyph = p.ToSvg(height);
            var bbox = p.GetBoundingRectangle();
            var bboxes = new List<PdfRectangle>();

            foreach (var command in p.Commands)
            {
                var segBbox = command.GetBoundingRectangle();
                if (segBbox.HasValue)
                {
                    bboxes.Add(segBbox.Value);
                }
            }

            var path = $"<path d='{glyph}' stroke='cyan' stroke-width='3'></path>";
            var bboxRect = bbox.HasValue ? BboxToRect(bbox.Value, "yellow") : string.Empty;
            var others = string.Join(" ", bboxes.Select(x => BboxToRect(x, "gray")));
            var result = $"<svg width='500' height='500'><g transform=\"scale(0.2, -0.2) translate(100, -700)\">{path} {bboxRect} {others}</g></svg>";

            return result;
        }
    }
}
