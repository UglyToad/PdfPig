namespace UglyToad.PdfPig.Geometry
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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
        internal static PdfRectangle ParametricPerpendicularProjection(IReadOnlyList<PdfPoint> polygon)
        {
            if (polygon == null || polygon.Count == 0)
            {
                throw new ArgumentException("ParametricPerpendicularProjection(): polygon cannot be null and must contain at least one point.");
            }

            if (polygon.Count < 4)
            {
                if (polygon.Count == 1)
                {
                    return new PdfRectangle(polygon[0], polygon[0]);
                }
                else if (polygon.Count == 2)
                {
                    return new PdfRectangle(polygon[0], polygon[1]);
                }
                else
                {
                    PdfPoint p3 = polygon[0].Add(polygon[1].Subtract(polygon[2]));
                    return new PdfRectangle(p3, polygon[1], polygon[0], polygon[2]);
                }
            }

            PdfPoint[] MBR = new PdfPoint[0];
            
            double Amin = double.MaxValue;
            double tmin = 1;
            double tmax = 0;
            double smax = 0;
            int j = 1;
            int k = 0;
            int l = -1;

            PdfPoint Q = new PdfPoint();
            PdfPoint R0 = new PdfPoint();
            PdfPoint R1 = new PdfPoint();
            PdfPoint u = new PdfPoint();

            int nv = polygon.Count;
            
            while (true)
            {
                var Pk = polygon[k];
                
                PdfPoint v = polygon[j].Subtract(Pk);
                double r = 1.0 / v.DotProduct(v);

                for (j = 0; j < nv; j++)
                {
                    if (j == k) continue;
                    PdfPoint Pj = polygon[j];
                    u = Pj.Subtract(Pk);
                    double t = u.DotProduct(v) * r;
                    PdfPoint Pt = new PdfPoint(t * v.X + Pk.X, t * v.Y + Pk.Y);
                    u = Pt.Subtract(Pj);
                    double s = u.DotProduct(u);

                    if (t < tmin)
                    {
                        tmin = t;
                        R0 = Pt;
                    }

                    if (t > tmax)
                    {
                        tmax = t;
                        R1 = Pt;
                    }

                    if (s > smax)
                    {
                        smax = s;
                        Q = Pt;
                        l = j;
                    }
                }

                if (l == -1)
                {
                    // All points are colinear - rectangle has no area (need more tests)
                    var bottomLeft = polygon.OrderBy(p => p.X).ThenBy(p => p.Y).First();
                    var topRight = polygon.OrderByDescending(p => p.Y).OrderByDescending(p => p.X).First();
                    return new PdfRectangle(bottomLeft, topRight, bottomLeft, topRight);
                }

                PdfPoint PlMinusQ = polygon[l].Subtract(Q);
                PdfPoint R2 = R1.Add(PlMinusQ);
                PdfPoint R3 = R0.Add(PlMinusQ);
                u = R1.Subtract(R0);
                double A = u.DotProduct(u) * smax;

                if (A < Amin)
                {
                    Amin = A;
                    MBR = new[] { R0, R1, R2, R3 };
                }

                k++;
                j = k;

                if (j == nv) j = 0;

                if (k == nv) break;
            }

            return new PdfRectangle(MBR[2], MBR[3], MBR[1], MBR[0]);
        }

        /// <summary>
        /// Algorithm to find the oriented bounding box (OBB) by first fitting a line through the points to get the slope,
        /// then rotating the points to obtain the axis-aligned bounding box (AABB), and then rotating back the AABB.
        /// </summary>
        /// <param name="points">The points.</param>
        internal static PdfRectangle OrientedBoundingBox(IReadOnlyList<PdfPoint> points)
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
            var angleRad = Math.Atan(slope);
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

            Func<PdfPoint, PdfPoint, double> polarAngle = (PdfPoint point1, PdfPoint point2) =>
            {
                return Math.Atan2(point2.Y - point1.Y, point2.X - point1.X) % Math.PI;
            };

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
        /// Whether the rectangle contains the point.
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
        /// Whether the rectangle contains the rectangle.
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
        /// Whether the line segment contains the point.
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
        public static bool IntersectsWith(this PdfLine line, PdfPath.Line other)
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
        public static PdfPoint? Intersect(this PdfLine line, PdfPath.Line other)
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
        public static bool ParallelTo(this PdfLine line, PdfPath.Line other)
        {
            return ParallelTo(line.Point1, line.Point2, other.From, other.To);
        }
        #endregion

        #region Path Line
        /// <summary>
        /// Whether the line segment contains the point.
        /// </summary>
        public static bool Contains(this PdfPath.Line line, PdfPoint point)
        {
            return Contains(line.From, line.To, point);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfPath.Line line, PdfPath.Line other)
        {
            return IntersectsWith(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfPath.Line line, PdfLine other)
        {
            return IntersectsWith(line.From, line.To, other.Point1, other.Point2);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfPath.Line line, PdfPath.Line other)
        {
            return Intersect(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfPath.Line line, PdfLine other)
        {
            return Intersect(line.From, line.To, other.Point1, other.Point2);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfPath.Line line, PdfPath.Line other)
        {
            return ParallelTo(line.From, line.To, other.From, other.To);
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfPath.Line line, PdfLine other)
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

        public static bool IntersectsWith(PdfPoint p11, PdfPoint p12, PdfPoint p21, PdfPoint p22)
        {
            return (ccw(p11, p12, p21) != ccw(p11, p12, p22)) &&
                   (ccw(p21, p22, p11) != ccw(p21, p22, p12));
        }

        private static PdfPoint? Intersect(PdfPoint p11, PdfPoint p12, PdfPoint p21, PdfPoint p22)
        {
            if (!IntersectsWith(p11, p12, p21, p22)) return null;

            var eq1 = GetSlopeIntercept(p11, p12);
            var eq2 = GetSlopeIntercept(p21, p22);

            if (double.IsNaN(eq1.Slope))
            {
                var x = eq1.Intercept;
                var y = eq2.Slope * x + eq2.Intercept;
                return new PdfPoint(x, y);
            }
            else if (double.IsNaN(eq2.Slope))
            {
                var x = eq2.Intercept;
                var y = eq1.Slope * x + eq1.Intercept;
                return new PdfPoint(x, y);
            }
            else
            {
                var x = (eq2.Intercept - eq1.Intercept) / (eq1.Slope - eq2.Slope);
                var y = eq1.Slope * x + eq1.Intercept;
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
        public static (PdfPath.BezierCurve, PdfPath.BezierCurve) Split(this PdfPath.BezierCurve bezierCurve, double tau)
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

            return (new PdfPath.BezierCurve(points[0][0], points[1][0], points[2][0], points[3][0]),
                    new PdfPath.BezierCurve(points[3][0], points[2][1], points[1][2], points[0][3]));
        }

        /// <summary>
        /// Checks if the curve and the line are intersecting.
        /// <para>Avoid using this method as it is not optimised. Use <see cref="Intersect(PdfPath.BezierCurve, PdfLine)"/> instead.</para>
        /// </summary>
        public static bool IntersectsWith(this PdfPath.BezierCurve bezierCurve, PdfLine line)
        {
            return IntersectsWith(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Checks if the curve and the line are intersecting.
        /// <para>Avoid using this method as it is not optimised. Use <see cref="Intersect(PdfPath.BezierCurve, PdfPath.Line)"/> instead.</para>
        /// </summary>
        public static bool IntersectsWith(this PdfPath.BezierCurve bezierCurve, PdfPath.Line line)
        {
            return IntersectsWith(bezierCurve, line.From, line.To);
        }

        private static bool IntersectsWith(PdfPath.BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
        {
            return Intersect(bezierCurve, p1, p2).Length > 0;
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        public static PdfPoint[] Intersect(this PdfPath.BezierCurve bezierCurve, PdfLine line)
        {
            return Intersect(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        public static PdfPoint[] Intersect(this PdfPath.BezierCurve bezierCurve, PdfPath.Line line)
        {
            return Intersect(bezierCurve, line.From, line.To);
        }
        
        private static PdfPoint[] Intersect(PdfPath.BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
        {
            var ts = IntersectT(bezierCurve, p1, p2);
            if (ts == null || ts.Length == 0) return EmptyArray<PdfPoint>.Instance;

            List<PdfPoint> points = new List<PdfPoint>();
            foreach (var t in ts)
            {
                PdfPoint point = new PdfPoint(
                    PdfPath.BezierCurve.ValueWithT(bezierCurve.StartPoint.X,
                                           bezierCurve.FirstControlPoint.X,
                                           bezierCurve.SecondControlPoint.X,
                                           bezierCurve.EndPoint.X,
                                           t),
                    PdfPath.BezierCurve.ValueWithT(bezierCurve.StartPoint.Y,
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
        /// <returns>List of t values where the <see cref="PdfPath.BezierCurve"/> and the <see cref="PdfLine"/> intersect.</returns>
        public static double[] IntersectT(this PdfPath.BezierCurve bezierCurve, PdfLine line)
        {
            return IntersectT(bezierCurve, line.Point1, line.Point2);
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="PdfPath.BezierCurve"/> and the <see cref="PdfPath.Line"/> intersect.</returns>
        public static double[] IntersectT(this PdfPath.BezierCurve bezierCurve, PdfPath.Line line)
        {
            return IntersectT(bezierCurve, line.From, line.To);
        }

        private static double[] IntersectT(PdfPath.BezierCurve bezierCurve, PdfPoint p1, PdfPoint p2)
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

        private static readonly double oneThird = 0.333333333333333333333;
        private static readonly double sqrtOfThree = 1.73205080756888;

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
            if (d < 0.0) return -Math.Pow(-d, oneThird);
            return Math.Pow(d, oneThird);
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
                double complexPart = sqrtOfThree / 2.0 * (S - T); // complex part of complex root
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
                Func<double, double, double, double> vietTrigonometricSolution = (p_, q_, k) => 2.0 * Math.Sqrt(-p_ / 3.0)
                        * Math.Cos(oneThird * Math.Acos((3.0 * q_) / (2.0 * p_) * Math.Sqrt(-3.0 / p_)) - (2.0 * Math.PI * k) / 3.0);

                double p = Q * 3.0;         // (3.0 * a * c - b * b) / (3.0 * aSquared);
                double q = -R * 2.0;        // (2.0 * bCubed - 9.0 * abc + 27.0 * aSquared * d) / (27.0 * aCubed);
                x1 = vietTrigonometricSolution(p, q, 0) - bOver3a;
                x2 = vietTrigonometricSolution(p, q, 1) - bOver3a;
                x3 = vietTrigonometricSolution(p, q, 2) - bOver3a;
            }

            return new[] {x1, x2, x3};
        }

        internal static string ToSvg(this PdfPath p)
        {
            var builder = new StringBuilder();
            foreach (var pathCommand in p.Commands)
            {
                pathCommand.WriteSvg(builder);
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

        internal static string ToFullSvg(this PdfPath p)
        {
            string BboxToRect(PdfRectangle box, string stroke)
            {
                var overallBbox = $"<rect x='{box.Left}' y='{box.Bottom}' width='{box.Width}' height='{box.Height}' stroke-width='2' fill='none' stroke='{stroke}'></rect>";
                return overallBbox;
            }

            var glyph = p.ToSvg();
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
