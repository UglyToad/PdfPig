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
        /// Algorithm to find the convex hull of the set of points with time complexity O(n log n).
        /// </summary>
        internal static IEnumerable<PdfPoint> GrahamScan(IEnumerable<PdfPoint> points)
        {
            if (points == null || points.Count() == 0)
            {
                throw new ArgumentException("GrahamScan(): points cannot be null and must contain at least one point.");
            }

            if (points.Count() < 3) return points;

            Func<PdfPoint, PdfPoint, PdfPoint, double> ccw = (PdfPoint p1, PdfPoint p2, PdfPoint p3) =>
            {
                return Math.Round((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X), 6);
            };

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
                while (ccw(stack.ElementAt(1), stack.Peek(), point) < 0)
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

        /// <summary>
        /// Whether two rectangles overlap.
        /// <para>Returns false if the two rectangles only share a border.</para>
        /// </summary>
        public static bool IntersectsWith(this PdfRectangle rectangle, PdfRectangle other)
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

        /// <summary>
        /// Get the <see cref="PdfRectangle"/> that is the intersection of two rectangles.
        /// </summary>
        public static PdfRectangle? Intersect(this PdfRectangle rectangle, PdfRectangle other)
        {
            if (!rectangle.IntersectsWith(other)) return null;
            return new PdfRectangle(Math.Max(rectangle.BottomLeft.X, other.BottomLeft.X),
                                    Math.Max(rectangle.BottomLeft.Y, other.BottomLeft.Y),
                                    Math.Min(rectangle.TopRight.X, other.TopRight.X),
                                    Math.Min(rectangle.TopRight.Y, other.TopRight.Y));
        }
        #endregion

        #region PdfLine
        /// <summary>
        /// Whether the line segment contains the point.
        /// </summary>
        public static bool Contains(this PdfLine line, PdfPoint point)
        {
            if (line.Point2.X == line.Point1.X)
            {
                if (point.X == line.Point2.X)
                {
                    return Math.Sign(point.Y - line.Point2.Y) != Math.Sign(point.Y - line.Point1.Y);
                }
                return false;
            }

            if (line.Point2.Y == line.Point1.Y)
            {
                if (point.Y == line.Point2.Y)
                {
                    return Math.Sign(point.X - line.Point2.X) != Math.Sign(point.X - line.Point1.X);
                }
                return false;
            }

            var tx = (point.X - line.Point1.X) / (line.Point2.X - line.Point1.X);
            var ty = (point.Y - line.Point1.Y) / (line.Point2.Y - line.Point1.Y);
            if (Math.Round(tx - ty, 5) != 0) return false;
            return (tx >= 0 && tx <= 1);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfLine line, PdfLine other)
        {
            return Intersect(line, other) != null;
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfLine line, PdfLine other)
        {
            // if the bounding boxes do not intersect, the lines cannot intersect
            if (!line.GetBoundingRectangle().IntersectsWith(other.GetBoundingRectangle()))
            {
                return null;
            }

            var eq1 = GetSlopeIntercept(line.Point1, line.Point2);
            var eq2 = GetSlopeIntercept(other.Point1, other.Point2);

            if (double.IsNaN(eq1.Slope) && double.IsNaN(eq2.Slope)) return null; // both lines are vertical (hence parallel)
            if (eq1.Slope == eq2.Slope) return null; // both lines are parallel

            var intersection = new PdfPoint();

            if (double.IsNaN(eq1.Slope))
            {
                var x = eq1.Intercept;
                var y = eq2.Slope * x + eq2.Intercept;
                intersection = new PdfPoint(x, y);
            }
            else if (double.IsNaN(eq2.Slope))
            {
                var x = eq2.Intercept;
                var y = eq1.Slope * x + eq1.Intercept;
                intersection = new PdfPoint(x, y);
            }
            else
            {
                var x = (eq2.Intercept - eq1.Intercept) / (eq1.Slope - eq2.Slope);
                var y = eq1.Slope * x + eq1.Intercept;
                intersection = new PdfPoint(x, y);
            }

            // check if the intersection point belongs to both segments 
            // (for the moment we only know it belongs to both lines)
            if (!line.Contains(intersection)) return null;
            if (!other.Contains(intersection)) return null;
            return intersection;
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfLine line, PdfLine other)
        {
            var val1 = (line.Point2.Y - line.Point1.Y) * (other.Point2.X - other.Point1.X);
            var val2 = (other.Point2.Y - other.Point1.Y) * (line.Point2.X - line.Point1.X);
            return Math.Round(val1 - val2, 5) == 0;
        }
        #endregion

        #region Path Line
        /// <summary>
        /// Whether the line segment contains the point.
        /// </summary>
        public static bool Contains(this PdfPath.Line line, PdfPoint point)
        {
            if (line.To.X == line.From.X)
            {
                if (point.X == line.To.X)
                {
                    return Math.Sign(point.Y - line.To.Y) != Math.Sign(point.Y - line.From.Y);
                }
                return false;
            }

            if (line.To.Y == line.From.Y)
            {
                if (point.Y == line.To.Y)
                {
                    return Math.Sign(point.X - line.To.X) != Math.Sign(point.X - line.From.X);
                }
                return false;
            }

            var tx = (point.X - line.From.X) / (line.To.X - line.From.X);
            var ty = (point.Y - line.From.Y) / (line.To.Y - line.From.Y);
            if (Math.Round(tx - ty, 5) != 0) return false;
            return (tx >= 0 && tx <= 1);
        }

        /// <summary>
        /// Whether two lines intersect.
        /// </summary>
        public static bool IntersectsWith(this PdfPath.Line line, PdfPath.Line other)
        {
            return Intersect(line, other) != null;
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this PdfPath.Line line, PdfPath.Line other)
        {
            // if the bounding boxes do not intersect, the lines cannot intersect
            var thisLineBbox = line.GetBoundingRectangle();
            if (!thisLineBbox.HasValue) return null;

            var lineBbox = other.GetBoundingRectangle();
            if (!lineBbox.HasValue) return null;

            if (!thisLineBbox.Value.IntersectsWith(lineBbox.Value))
            {
                return null;
            }

            var eq1 = GetSlopeIntercept(line.From, line.To);
            var eq2 = GetSlopeIntercept(other.From, other.To);

            if (double.IsNaN(eq1.Slope) && double.IsNaN(eq2.Slope)) return null; // both lines are vertical (hence parallel)
            if (eq1.Slope == eq2.Slope) return null; // both lines are parallel

            var intersection = new PdfPoint();

            if (double.IsNaN(eq1.Slope))
            {
                var x = eq1.Intercept;
                var y = eq2.Slope * x + eq2.Intercept;
                intersection = new PdfPoint(x, y);
            }
            else if (double.IsNaN(eq2.Slope))
            {
                var x = eq2.Intercept;
                var y = eq1.Slope * x + eq1.Intercept;
                intersection = new PdfPoint(x, y);
            }
            else
            {
                var x = (eq2.Intercept - eq1.Intercept) / (eq1.Slope - eq2.Slope);
                var y = eq1.Slope * x + eq1.Intercept;
                intersection = new PdfPoint(x, y);
            }

            // check if the intersection point belongs to both segments 
            // (for the moment we only know it belongs to both lines)
            if (!line.Contains(intersection)) return null;
            if (!other.Contains(intersection)) return null;
            return intersection;
        }

        /// <summary>
        /// Checks if both lines are parallel.
        /// </summary>
        public static bool ParallelTo(this PdfPath.Line line, PdfPath.Line other)
        {
            var val1 = (line.To.Y - line.From.Y) * (other.To.X - other.From.X);
            var val2 = (other.To.Y - other.From.Y) * (line.To.X - line.From.X);
            return Math.Round(val1 - val2, 5) == 0;
        }
        #endregion

        #region Path Bezier Curve
        /// <summary>
        /// Split a bezier curve into 2 bezier curves, at tau.
        /// </summary>
        /// <param name="bezierCurve">The original bezier curve.</param>
        /// <param name="tau">The t value were to split the curve, usually between 0 and 1, but not necessary.</param>
        private static (PdfPath.BezierCurve, PdfPath.BezierCurve) Split(this PdfPath.BezierCurve bezierCurve, double tau)
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
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        /// <returns></returns>
        public static PdfPoint[] Intersect(this PdfPath.BezierCurve bezierCurve, PdfLine line)
        {
            var ts = FindIntersectionT(bezierCurve, line);
            if (!ts.Any()) return null;

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
                points.Add(point);
            }
            return points.ToArray();
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        /// <returns></returns>
        public static PdfPoint[] Intersect(this PdfPath.BezierCurve bezierCurve, PdfPath.Line line)
        {
            var ts = FindIntersectionT(bezierCurve, line);
            if (ts.Count() == 0) return null;

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
                                           t)
                                           );
                points.Add(point);
            }
            return points.ToArray();
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="PdfPath.BezierCurve"/> and the <see cref="PdfLine"/> intersect.</returns>
        public static double[] FindIntersectionT(this PdfPath.BezierCurve bezierCurve, PdfLine line)
        {
            // if the bounding boxes do not intersect, they cannot intersect
            var bezierBbox = bezierCurve.GetBoundingRectangle();
            if (!bezierBbox.HasValue) return null;
            var lineBbox = line.GetBoundingRectangle();

            if (!bezierBbox.Value.IntersectsWith(lineBbox))
            {
                return null;
            }

            double x1 = line.Point1.X;
            double y1 = line.Point1.Y;
            double x2 = line.Point2.X;
            double y2 = line.Point2.Y;
            return FindIntersectionT(bezierCurve, x1, y1, x2, y2);
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="PdfPath.BezierCurve"/> and the <see cref="PdfPath.Line"/> intersect.</returns>
        public static double[] FindIntersectionT(this PdfPath.BezierCurve bezierCurve, PdfPath.Line line)
        {
            // if the bounding boxes do not intersect, they cannot intersect
            var bezierBbox = bezierCurve.GetBoundingRectangle();
            if (!bezierBbox.HasValue) return null;
            var lineBbox = line.GetBoundingRectangle();
            if (!lineBbox.HasValue) return null;

            if (!bezierBbox.Value.IntersectsWith(lineBbox.Value))
            {
                return null;
            }

            double x1 = line.From.X;
            double y1 = line.From.Y;
            double x2 = line.To.X;
            double y2 = line.To.Y;
            return FindIntersectionT(bezierCurve, x1, y1, x2, y2);
        }

        private static double[] FindIntersectionT(PdfPath.BezierCurve bezierCurve, double x1, double y1, double x2, double y2)
        {
            double A = (y2 - y1);
            double B = (x1 - x2);
            double C = x1 * (y1 - y2) + y1 * (x2 - x1);

            double alpha = bezierCurve.StartPoint.X * A + bezierCurve.StartPoint.Y * B;
            double beta = 3.0 * (bezierCurve.FirstControlPoint.X * A + bezierCurve.FirstControlPoint.Y * B);
            double gamma = 3.0 * (bezierCurve.SecondControlPoint.X * A + bezierCurve.SecondControlPoint.Y * B);
            double delta = bezierCurve.EndPoint.X * A + bezierCurve.EndPoint.Y * B;

            double a = (-alpha + beta - gamma + delta);
            double b = (3 * alpha - 2 * beta + gamma);
            double c = -3 * alpha + beta;
            double d = alpha + C;

            var solution = SolveCubicEquation(a, b, c, d);

            return solution.Where(s => !double.IsNaN(s)).Where(s => s >= -double.Epsilon && s <= 1.0).OrderBy(s => s).ToArray();
        }
        #endregion

        private static readonly double oneThird = 0.333333333333333333333;

        private static (double Slope, double Intercept) GetSlopeIntercept(PdfPoint point1, PdfPoint point2)
        {
            if ((point1.X - point2.X) != 0) // vertical line special case
            {
                var slope = (point2.Y - point1.Y) / (point2.X - point1.X);
                var intercept = point2.Y - slope * point2.X;
                return (slope, intercept);
            }
            else
            {
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
            if (Math.Abs(a) <= double.Epsilon)
            {
                // handle Quadratic equation (a=0)
                double detQ = c * c - 4 * b * d;
                if (detQ >= 0)
                {
                    double x = (-c + Math.Sqrt(detQ)) / (2.0 * b);
                    double x0 = (-c - Math.Sqrt(detQ)) / (2.0 * b);
                    return new double[] { x, x0 };
                }
                return new double[0]; // no real roots
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
                double complexPart = Math.Sqrt(3) / 2.0 * (S - T); // complex part of complex root
                if (Math.Abs(complexPart) <= double.Epsilon) // if complex part == 0
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
