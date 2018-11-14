// ReSharper disable ArrangeRedundantParentheses
namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Geometry;

    internal class CharacterPath
    {
        private readonly List<IPathCommand> commands = new List<IPathCommand>();
        private PdfPoint? currentPosition;

        public void MoveTo(decimal x, decimal y)
        {
            currentPosition = new PdfPoint(x, y);
            commands.Add(new Move(currentPosition.Value));
        }

        public void LineTo(decimal x, decimal y)
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

        public void QuadraticCurveTo(decimal x1, decimal y1, decimal x2, decimal y2) { }

        public void BezierCurveTo(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
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

        public void SetWindingRuleMode(int windingRule) { }

        public void ClosePath()
        {
            commands.Add(new Close());
        }

        public PdfRectangle GetBoundingRectangle()
        {
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

        public string ToSvg()
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

        public string ToFullSvg()
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
            var bboxRect = BboxToRect(bbox, "yellow");
            var others = string.Join(" ", bboxes.Select(x => BboxToRect(x, "gray")));
            var result = $"<svg transform='scale(1, -1)' width='2000' height='2000'>{path} {bboxRect} {others}</svg>";

            return result;
        }

        public interface IPathCommand
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

        public class BezierCurve : IPathCommand
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
                var minX = Math.Min(StartPoint.X, EndPoint.X);
                var maxX = Math.Max(StartPoint.X, EndPoint.X);

                var minY = Math.Min(StartPoint.Y, EndPoint.Y);
                var maxY = Math.Max(StartPoint.Y, EndPoint.Y);

                if (TrySolveQuadratic(x => (double)x.X, minX, maxX, out var xSolutions))
                {
                    minX = xSolutions.min;
                    maxX = xSolutions.max;
                }

                if (TrySolveQuadratic(x => (double)x.Y, minY, maxY, out var ySolutions))
                {
                    minY = ySolutions.min;
                    maxY = ySolutions.max;
                }

                return new PdfRectangle(minX, minY, maxX, maxY);
            }


            private bool TrySolveQuadratic(Func<PdfPoint, double> valueAccessor, decimal currentMin, decimal currentMax, out (decimal min, decimal max) solutions)
            {
                solutions = default((decimal, decimal));

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
                var p1 = valueAccessor(StartPoint);
                var p2 = valueAccessor(FirstControlPoint);
                var p3 = valueAccessor(SecondControlPoint);
                var p4 = valueAccessor(EndPoint);

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

                var t1 = (-b + Math.Sqrt(sqrtable)) / (2 * a);
                var t2 = (-b - Math.Sqrt(sqrtable)) / (2 * a);

                if (t1 >= 0 && t1 <= 1)
                {
                    var sol1 = (decimal)ValueWithT(p1, p2, p3, p4, t1);
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
                    var sol2 = (decimal)ValueWithT(p1, p2, p3, p4, t2);
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
                var p = (Math.Pow(1 - t, 3) * p1) + (3 * Math.Pow(1 - t, 2) * t * p2) + (3 * (1 - t) * Math.Pow(t, 2) * p3) + (Math.Pow(t, 3) * p4);

                return p;
            }

            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("C {0} {1}, {2} {3}, {4} {5} ", FirstControlPoint.X, FirstControlPoint.Y, SecondControlPoint.X, SecondControlPoint.Y,
                    EndPoint.X, EndPoint.Y);
            }
        }
    }
}
