using System;
using System.Collections.Generic;
using System.Linq;
using static UglyToad.PdfPig.Geometry.PdfPath;

namespace UglyToad.PdfPig.Geometry
{
    /// <summary>
    /// Extension class to Geometry.
    /// </summary>
    public static class GeometryExtensions
    {
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
        public static bool Contains(this Line line, PdfPoint point)
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
        public static bool IntersectsWith(this Line line, Line other)
        {
            return Intersect(line, other) != null;
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/> that is the intersection of two lines.
        /// </summary>
        public static PdfPoint? Intersect(this Line line, Line other)
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
        public static bool ParallelTo(this Line line, Line other)
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
        private static (BezierCurve, BezierCurve) Split(this BezierCurve bezierCurve, double tau)
        {
            // De Casteljau Algorithm
            PdfPoint[][] points = new PdfPoint[4][];

            points[0] = new []
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
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        /// <returns></returns>
        public static PdfPoint[] Intersect(this BezierCurve bezierCurve, PdfLine line)
        {
            var ts = FindIntersectionT(bezierCurve, line);
            if (!ts.Any()) return null;

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
                points.Add(point);
            }
            return points.ToArray();
        }

        /// <summary>
        /// Get the <see cref="PdfPoint"/>s that are the intersections of the line and the curve.
        /// </summary>
        /// <returns></returns>
        public static PdfPoint[] Intersect(this BezierCurve bezierCurve, Line line)
        {
            var ts = FindIntersectionT(bezierCurve, line);
            if (ts.Count() == 0) return null;

            List<PdfPoint> points = new List<PdfPoint>();
            foreach (var t in ts)
            {
                PdfPoint point = new PdfPoint(
                    BezierCurve.ValueWithT((double)bezierCurve.StartPoint.X,
                                           (double)bezierCurve.FirstControlPoint.X,
                                           (double)bezierCurve.SecondControlPoint.X,
                                           (double)bezierCurve.EndPoint.X,
                                           t),
                    BezierCurve.ValueWithT((double)bezierCurve.StartPoint.Y,
                                           (double)bezierCurve.FirstControlPoint.Y,
                                           (double)bezierCurve.SecondControlPoint.Y,
                                           (double)bezierCurve.EndPoint.Y,
                                           t)
                                           );
                points.Add(point);
            }
            return points.ToArray();
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="BezierCurve"/> and the <see cref="PdfLine"/> intersect.</returns>
        public static double[] FindIntersectionT(this BezierCurve bezierCurve, PdfLine line)
        {
            // if the bounding boxes do not intersect, they cannot intersect
            var bezierBbox = bezierCurve.GetBoundingRectangle();
            if (!bezierBbox.HasValue) return null;
            var lineBbox = line.GetBoundingRectangle();

            if (!bezierBbox.Value.IntersectsWith(lineBbox))
            {
                return null;
            }

            double x1 = (double)line.Point1.X;
            double y1 = (double)line.Point1.Y;
            double x2 = (double)line.Point2.X;
            double y2 = (double)line.Point2.Y;
            return FindIntersectionT(bezierCurve, x1, y1, x2, y2);
        }

        /// <summary>
        /// Get the t values that are the intersections of the line and the curve.
        /// </summary>
        /// <returns>List of t values where the <see cref="BezierCurve"/> and the <see cref="Line"/> intersect.</returns>
        public static double[] FindIntersectionT(this BezierCurve bezierCurve, Line line)
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

            double x1 = (double)line.From.X;
            double y1 = (double)line.From.Y;
            double x2 = (double)line.To.X;
            double y2 = (double)line.To.Y;
            return FindIntersectionT(bezierCurve, x1, y1, x2, y2);
        }

        private static double[] FindIntersectionT(BezierCurve bezierCurve, double x1, double y1, double x2, double y2)
        {
            double A = (y2 - y1);
            double B = (x1 - x2);
            double C = x1 * (y1 - y2) + y1 * (x2 - x1);

            double alpha = (double)bezierCurve.StartPoint.X * A + (double)bezierCurve.StartPoint.Y * B;
            double beta = 3.0 * ((double)bezierCurve.FirstControlPoint.X * A + (double)bezierCurve.FirstControlPoint.Y * B);
            double gamma = 3.0 * ((double)bezierCurve.SecondControlPoint.X * A + (double)bezierCurve.SecondControlPoint.Y * B);
            double delta = (double)bezierCurve.EndPoint.X * A + (double)bezierCurve.EndPoint.Y * B;

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
                var slope = (double)((point2.Y - point1.Y) / (point2.X - point1.X));
                var intercept = (double)point2.Y - slope * (double)point2.X;
                return (slope, intercept);
            }
            else
            {
                return (double.NaN, (double)point1.X);
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

            return new double[] { x1, x2, x3 };
        }
    }
}
