// ReSharper disable ArrangeRedundantParentheses
namespace UglyToad.PdfPig.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A path in a PDF document, used by glyphs and page content.
    /// </summary>
    public class PdfPath
    {
        private readonly List<IPathCommand> commands = new List<IPathCommand>();
        private PdfPoint? currentPosition;

        internal void MoveTo(decimal x, decimal y)
        {
            currentPosition = new PdfPoint(x, y);
            commands.Add(new Move(currentPosition.Value));
        }

        internal void LineTo(decimal x, decimal y)
        {
            if (currentPosition.HasValue)
            {
                var to = new PdfPoint(x, y);
                commands.Add(new Line(currentPosition.Value, to));
                currentPosition = to;
            }
            else
            {
                MoveTo(x, y);
            }
        }

        internal void QuadraticCurveTo(decimal x1, decimal y1, decimal x2, decimal y2) { }

        internal void BezierCurveTo(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            if (currentPosition.HasValue)
            {
                var to = new PdfPoint(x3, y3);
                commands.Add(new BezierCurve(currentPosition.Value,
                    new PdfPoint(x1, y1), new PdfPoint(x2, y2), to));
                currentPosition = to;
            }
            else
            {
                MoveTo(x3, y3);
            }
        }

        internal void SetWindingRuleMode(int windingRule) { }

        internal void ClosePath()
        {
            commands.Add(new Close());
        }

        internal PdfRectangle? GetBoundingRectangle()
        {
            if (commands.Count == 0)
            {
                return null;
            }

            var minX = decimal.MaxValue;
            var maxX = decimal.MinValue;

            var minY = decimal.MaxValue;
            var maxY = decimal.MinValue;

            foreach (var command in commands)
            {
                var rect = command.GetBoundingRectangle();
                if (rect == null)
                {
                    continue;
                }

                if (rect.Value.Left < minX)
                {
                    minX = rect.Value.Left;
                }

                if (rect.Value.Right > maxX)
                {
                    maxX = rect.Value.Right;
                }

                if (rect.Value.Bottom < minY)
                {
                    minY = rect.Value.Bottom;
                }

                if (rect.Value.Top > maxY)
                {
                    maxY = rect.Value.Top;
                }
            }

            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        internal string ToSvg()
        {
            var builder = new StringBuilder();
            foreach (var pathCommand in commands)
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

        internal string ToFullSvg()
        {
            string BboxToRect(PdfRectangle box, string stroke)
            {
                var overallBbox = $"<rect x='{box.Left}' y='{box.Bottom}' width='{box.Width}' height='{box.Height}' stroke-width='2' fill='none' stroke='{stroke}'></rect>";
                return overallBbox;
            }

            var glyph = ToSvg();
            var bbox = GetBoundingRectangle();
            var bboxes = new List<PdfRectangle>();

            foreach (var command in commands)
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

        internal interface IPathCommand
        {
            PdfRectangle? GetBoundingRectangle();

            void WriteSvg(StringBuilder builder);
        }

        private class Close : IPathCommand
        {
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("Z ");
            }
        }

        private class Move : IPathCommand
        {
            public PdfPoint Location { get; }

            public Move(PdfPoint location)
            {
                Location = location;
            }

            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("M ").Append(Location.X).Append(' ').Append(Location.Y).Append(' ');
            }
        }

        private class Line : IPathCommand
        {
            public PdfPoint From { get; }

            public PdfPoint To { get; }

            public Line(PdfPoint from, PdfPoint to)
            {
                From = from;
                To = to;
            }

            public PdfRectangle? GetBoundingRectangle()
            {
                return new PdfRectangle(From, To);
            }

            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("L {0} {1} ", To.X, To.Y);
            }
        }

        internal class BezierCurve : IPathCommand
        {
            public PdfPoint StartPoint { get; }

            public PdfPoint FirstControlPoint { get; }

            public PdfPoint SecondControlPoint { get; }

            public PdfPoint EndPoint { get; }

            public BezierCurve(PdfPoint startPoint, PdfPoint firstControlPoint, PdfPoint secondControlPoint, PdfPoint endPoint)
            {
                StartPoint = startPoint;
                FirstControlPoint = firstControlPoint;
                SecondControlPoint = secondControlPoint;
                EndPoint = endPoint;
            }

            public PdfRectangle? GetBoundingRectangle()
            {
                // Optimised
                double minX;
                double maxX;
                if (StartPoint.X <= EndPoint.X)
                {
                    minX = (double)StartPoint.X;
                    maxX = (double)EndPoint.X;
                }
                else
                {
                    minX = (double)EndPoint.X;
                    maxX = (double)StartPoint.X;
                }

                double minY;
                double maxY;
                if (StartPoint.Y <= EndPoint.Y)
                {
                    minY = (double)StartPoint.Y;
                    maxY = (double)EndPoint.Y;
                }
                else
                {
                    minY = (double)EndPoint.Y;
                    maxY = (double)StartPoint.Y;
                }

                if (TrySolveQuadratic(true, minX, maxX, out var xSolutions))
                {
                    minX = xSolutions.min;
                    maxX = xSolutions.max;
                }

                if (TrySolveQuadratic(false, minY, maxY, out var ySolutions))
                {
                    minY = ySolutions.min;
                    maxY = ySolutions.max;
                }

                return new PdfRectangle((decimal)minX, (decimal)minY, (decimal)maxX, (decimal)maxY);
            }


            private bool TrySolveQuadratic(bool isX, double currentMin, double currentMax, out (double min, double max) solutions)
            {
                solutions = default((double, double));

                // This method has been optimised for performance by eliminating calls to Math.

                // Given k points the general form is:
                // P = (1-t)^(k - i - 1)*t^(i)*P_i
                // 
                // For 4 points this gives:
                // P = (1−t)^3*P_1 + 3(1−t)^2*t*P_2 + 3(1−t)*t^2*P_3 + t^3*P_4
                // The differential is:
                // P' = 3(1-t)^2(P_2 - P_1) + 6(1-t)^t(P_3 - P_2) + 3t^2(P_4 - P_3)

                // P' = 3da(1-t)^2 + 6db(1-t)t + 3dct^2
                // P' = 3da - 3dat - 3dat + 3dat^2 + 6dbt - 6dbt^2 + 3dct^2
                // P' = (3da - 6db + 3dc)t^2 + (6db - 3da - 3da)t + 3da
                var p1 = (double)(isX ? StartPoint.X : StartPoint.Y);
                var p2 = (double)(isX ? FirstControlPoint.X : FirstControlPoint.Y);
                var p3 = (double)(isX ? SecondControlPoint.X : SecondControlPoint.Y);
                var p4 = (double)(isX ? EndPoint.X : EndPoint.Y);

                var threeda = 3 * (p2 - p1);
                var sixdb = 6 * (p3 - p2);
                var threedc = 3 * (p4 - p3);

                var a = threeda - sixdb + threedc;
                var b = sixdb - threeda - threeda;
                var c = threeda;

                // P' = at^2 + bt + c
                // t = (-b (+/-) sqrt(b ^ 2 - 4ac))/2a

                var sqrtable = (b * b) - (4 * a * c);

                if (sqrtable < 0)
                {
                    return false;
                }

                var sqrt = Math.Sqrt(sqrtable);
                var divisor = 2 * a;

                var t1 = (-b + sqrt) / divisor;
                var t2 = (-b - sqrt) / divisor;

                if (t1 >= 0 && t1 <= 1)
                {
                    var sol1 = ValueWithT(p1, p2, p3, p4, t1);
                    if (sol1 < currentMin)
                    {
                        currentMin = sol1;
                    }

                    if (sol1 > currentMax)
                    {
                        currentMax = sol1;
                    }
                }

                if (t2 >= 0 && t2 <= 1)
                {
                    var sol2 = ValueWithT(p1, p2, p3, p4, t2);
                    if (sol2 < currentMin)
                    {
                        currentMin = sol2;
                    }

                    if (sol2 > currentMax)
                    {
                        currentMax = sol2;
                    }
                }

                solutions = (currentMin, currentMax);

                return true;
            }

            private static double ValueWithT(double p1, double p2, double p3, double p4, double t)
            {
                // P = (1−t)^3*P_1 + 3(1−t)^2*t*P_2 + 3(1−t)*t^2*P_3 + t^3*P_4
                var oneMinusT = 1 - t;
                var p = ((oneMinusT * oneMinusT * oneMinusT) * p1)
                        + (3 * (oneMinusT * oneMinusT) * t * p2)
                        + (3 * oneMinusT * (t * t) * p3)
                        + ((t * t * t) * p4);

                return p;
            }

            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("C {0} {1}, {2} {3}, {4} {5} ", FirstControlPoint.X, FirstControlPoint.Y, SecondControlPoint.X, SecondControlPoint.Y,
                    EndPoint.X, EndPoint.Y);
            }
        }

        internal void Rectangle(decimal x, decimal y, decimal width, decimal height)
        {
        }
    }
}
