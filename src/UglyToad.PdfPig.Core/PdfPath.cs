namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A path in a PDF document, used by glyphs and page content. Can contain multiple sub-paths.
    /// </summary>
    public class PdfPath
    {
        private readonly List<IPathCommand> commands = new List<IPathCommand>();

        /// <summary>
        /// The sequence of sub-paths which form this <see cref="PdfPath"/>.
        /// </summary>
        public IReadOnlyList<IPathCommand> Commands => commands;

        /// <summary>
        /// True if the <see cref="PdfPath"/> was originaly draw as a rectangle.
        /// </summary>
        public bool IsDrawnAsRectangle { get; internal set; }

        private PdfPoint? currentPosition;

        private double shoeLaceSum;

        /// <summary>
        /// Return true if it is a closed path.
        /// </summary>
        /// <returns></returns>
        public bool IsClosed()
        {
            // need to check if filled -> true if filled
            if (Commands.Any(c => c is Close)) return true;
            var filtered = Commands.Where(c => c is Line || c is BezierCurve).ToList();
            if (filtered.Count < 2) return false;
            if (!GetStartPoint(filtered.First()).Equals(GetEndPoint(filtered.Last()))) return false;
            return true;
        }

        /// <summary>
        /// Return true if points are organised in a clockwise order. Works only with closed paths.
        /// </summary>
        /// <returns></returns>
        public bool IsClockwise
        {
            get
            {
                if (!IsClosed()) return false;
                return shoeLaceSum > 0;
            }
        }

        /// <summary>
        /// Return true if points are organised in a counterclockwise order. Works only with closed paths.
        /// </summary>
        /// <returns></returns>
        public bool IsCounterClockwise
        {
            get
            {
                if (!IsClosed()) return false;
                return shoeLaceSum < 0;
            }
        }

        /// <summary>
        /// Get the <see cref="PdfPath"/>'s centroid point.
        /// </summary>
        public PdfPoint GetCentroid()
        {
            var filtered = commands.Where(c => c is Line || c is BezierCurve).ToList();
            if (filtered.Count == 0) return new PdfPoint();
            var points = filtered.Select(GetStartPoint).ToList();
            points.AddRange(filtered.Select(GetEndPoint));
            return new PdfPoint(points.Average(p => p.X), points.Average(p => p.Y));
        }

        internal static PdfPoint GetStartPoint(IPathCommand command)
        {
            if (command is Line line)
            {
                return line.From;
            }

            if (command is BezierCurve curve)
            {
                return curve.StartPoint;
            }

            if (command is Move move)
            {
                return move.Location;
            }

            throw new ArgumentException();
        }

        internal static PdfPoint GetEndPoint(IPathCommand command)
        {
            if (command is Line line)
            {
                return line.To;
            }

            if (command is BezierCurve curve)
            {
                return curve.EndPoint;
            }

            if (command is Move move)
            {
                return move.Location;
            }

            throw new ArgumentException();
        }

        /// <summary>
        /// Simplify this <see cref="PdfPath"/> by converting everything to <see cref="PdfLine"/>s.
        /// </summary>
        /// <param name="n">Number of lines required (minimum is 1).</param>
        internal PdfPath Simplify(int n = 4)
        {
            PdfPath simplifiedPath = new PdfPath();
            var startPoint = GetStartPoint(Commands.First());
            simplifiedPath.MoveTo(startPoint.X, startPoint.Y);

            foreach (var command in Commands)
            {
                if (command is Line line)
                {
                    simplifiedPath.LineTo(line.To.X, line.To.Y);
                }
                else if (command is BezierCurve curve)
                {
                    foreach (var lineB in curve.ToLines(n))
                    {
                        simplifiedPath.LineTo(lineB.To.X, lineB.To.Y);
                    }
                }
            }

            // Check if Closed, if yes: make sure it is actually closed (last TO = first FROM)
            if (IsClosed())
            {
                var first = GetStartPoint(simplifiedPath.Commands.First());
                if (!first.Equals(GetEndPoint(simplifiedPath.Commands.Last())))
                {
                    simplifiedPath.LineTo(first.X, first.Y);
                }
            }

            return simplifiedPath;
        }

        /// <summary>
        /// Add a <see cref="Move"/> command to the path.
        /// </summary>
        public void MoveTo(double x, double y)
        {
            currentPosition = new PdfPoint(x, y);
            commands.Add(new Move(currentPosition.Value));
        }

        /// <summary>
        /// Add a <see cref="Line"/> command to the path.
        /// </summary>
        public void LineTo(double x, double y)
        {
            if (currentPosition.HasValue)
            {
                shoeLaceSum += ((x - currentPosition.Value.X) * (y + currentPosition.Value.Y));

                var to = new PdfPoint(x, y);
                commands.Add(new Line(currentPosition.Value, to));
                currentPosition = to;
            }
            else
            {
                // TODO: probably the wrong behaviour here, maybe line starts from (0, 0)?
                MoveTo(x, y);
            }
        }
        
        /// <summary>
        /// Adds 4 <see cref="Line"/>s forming a rectangle to the path.
        /// </summary>
        public void Rectangle(double x, double y, double width, double height)
        {
            currentPosition = new PdfPoint(x, y);
            LineTo(x + width, y);
            LineTo(x + width, y + height);
            LineTo(x, y + height);
            LineTo(x, y);
            IsDrawnAsRectangle = true;
        }
        
        internal void QuadraticCurveTo(double x1, double y1, double x2, double y2) { }

        /// <summary>
        /// Add a <see cref="BezierCurve"/> to the path.
        /// </summary>
        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (currentPosition.HasValue)
            {
                shoeLaceSum += (x1 - currentPosition.Value.X) * (y1 + currentPosition.Value.Y);
                shoeLaceSum += (x2 - x1) * (y2 + y1);
                shoeLaceSum += (x3 - x2) * (y3 + y2);

                var to = new PdfPoint(x3, y3);
                commands.Add(new BezierCurve(currentPosition.Value, new PdfPoint(x1, y1), new PdfPoint(x2, y2), to));
                currentPosition = to;
            }
            else
            {
                MoveTo(x3, y3);
            }
        }

        internal void SetWindingRuleMode(int windingRule) { }

        /// <summary>
        /// Close the path.
        /// </summary>
        public void ClosePath()
        {
            if (currentPosition.HasValue)
            {
                var startPoint = GetStartPoint(commands.First());
                if (!startPoint.Equals(currentPosition.Value))
                {
                    shoeLaceSum += (startPoint.X - currentPosition.Value.X) * (startPoint.Y + currentPosition.Value.Y);
                }
            }
            commands.Add(new Close());
        }

        /// <summary>
        /// Gets a <see cref="PdfRectangle"/> which entirely contains the geometry of the defined path.
        /// </summary>
        /// <returns>For paths which don't define any geometry this returns <see langword="null"/>.</returns>
        public PdfRectangle? GetBoundingRectangle()
        {
            if (commands.Count == 0)
            {
                return null;
            }

            var minX = double.MaxValue;
            var maxX = double.MinValue;

            var minY = double.MaxValue;
            var maxY = double.MinValue;

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

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (minX == double.MaxValue ||
                maxX == double.MinValue ||
                minY == double.MaxValue ||
                maxY == double.MinValue)
            {
                return null;
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator

            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// A command in a <see cref="PdfPath"/>.
        /// </summary>
        public interface IPathCommand
        {
            /// <summary>
            /// Returns the smallest rectangle which contains the path region given by this command.
            /// </summary>
            /// <returns></returns>
            PdfRectangle? GetBoundingRectangle();

            /// <summary>
            /// Converts from the path command to an SVG string representing the path operation.
            /// </summary>
            void WriteSvg(StringBuilder builder);
        }

        /// <summary>
        /// Close the current <see cref="PdfPath"/>.
        /// </summary>
        public class Close : IPathCommand
        {
            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("Z ");
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return (obj is Close);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// Move drawing of the current <see cref="PdfPath"/> to the specified location.
        /// </summary>
        public class Move : IPathCommand
        {
            /// <summary>
            /// The location to move to.
            /// </summary>
            public PdfPoint Location { get; }

            /// <summary>
            /// Create a new <see cref="Move"/> path command.
            /// </summary>
            /// <param name="location"></param>
            public Move(PdfPoint location)
            {
                Location = location;
            }

            /// <summary>
            /// Returns <see langword="null"/> since this generates no visible path.
            /// </summary>
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.Append("M ").Append(Location.X).Append(' ').Append(Location.Y).Append(' ');
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is Move move)
                {
                    return Location.Equals(move.Location);
                }
                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return (Location).GetHashCode();
            }
        }

        /// <summary>
        /// Draw a straight line between two points.
        /// </summary>
        public class Line : IPathCommand
        {
            /// <summary>
            /// The start of the line.
            /// </summary>
            public PdfPoint From { get; }

            /// <summary>
            /// The end of the line.
            /// </summary>
            public PdfPoint To { get; }

            /// <summary>
            /// Create a new <see cref="Line"/>.
            /// </summary>
            public Line(PdfPoint from, PdfPoint to)
            {
                From = from;
                To = to;
            }

            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                return new PdfRectangle(From, To);
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("L {0} {1} ", To.X, To.Y);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is Line line)
                {
                    return From.Equals(line.From) && To.Equals(line.To);
                }
                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return (From, To).GetHashCode();
            }
        }

        /// <summary>
        /// Draw a Bezier curve given by the start, control and end points.
        /// </summary>
        public class BezierCurve : IPathCommand
        {
            /// <summary>
            /// The start point of the Bezier curve.
            /// </summary>
            public PdfPoint StartPoint { get; }

            /// <summary>
            /// The first control point of the curve.
            /// </summary>
            public PdfPoint FirstControlPoint { get; }

            /// <summary>
            /// The second control point of the curve.
            /// </summary>
            public PdfPoint SecondControlPoint { get; }

            /// <summary>
            /// The end point of the curve.
            /// </summary>
            public PdfPoint EndPoint { get; }

            /// <summary>
            /// Create a Bezier curve at the provided points.
            /// </summary>
            public BezierCurve(PdfPoint startPoint, PdfPoint firstControlPoint, PdfPoint secondControlPoint, PdfPoint endPoint)
            {
                StartPoint = startPoint;
                FirstControlPoint = firstControlPoint;
                SecondControlPoint = secondControlPoint;
                EndPoint = endPoint;
            }

            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                // Optimised
                double minX;
                double maxX;
                if (StartPoint.X <= EndPoint.X)
                {
                    minX = StartPoint.X;
                    maxX = EndPoint.X;
                }
                else
                {
                    minX = EndPoint.X;
                    maxX = StartPoint.X;
                }

                double minY;
                double maxY;
                if (StartPoint.Y <= EndPoint.Y)
                {
                    minY = StartPoint.Y;
                    maxY = EndPoint.Y;
                }
                else
                {
                    minY = EndPoint.Y;
                    maxY = StartPoint.Y;
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

                return new PdfRectangle(minX, minY, maxX, maxY);
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("C {0} {1}, {2} {3}, {4} {5} ", FirstControlPoint.X, FirstControlPoint.Y, SecondControlPoint.X, SecondControlPoint.Y,
                    EndPoint.X, EndPoint.Y);
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
                var p1 = isX ? StartPoint.X : StartPoint.Y;
                var p2 = isX ? FirstControlPoint.X : FirstControlPoint.Y;
                var p3 = isX ? SecondControlPoint.X : SecondControlPoint.Y;
                var p4 = isX ? EndPoint.X : EndPoint.Y;

                var threeda = 3 * (p2 - p1);
                var sixdb = 6 * (p3 - p2);
                var threedc = 3 * (p4 - p3);

                var a = threeda - sixdb + threedc;
                var b = sixdb - threeda - threeda;
                var c = threeda;

                // P' = at^2 + bt + c
                // t = (-b (+/-) sqrt(b ^ 2 - 4ac))/2a

                var sqrtable = b * b - 4 * a * c;

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

            /// <summary>
            /// Calculate the value of the Bezier curve at t.
            /// </summary>
            public static double ValueWithT(double p1, double p2, double p3, double p4, double t)
            {
                // P = (1−t)^3*P_1 + 3(1−t)^2*t*P_2 + 3(1−t)*t^2*P_3 + t^3*P_4
                var oneMinusT = 1 - t;
                var p = ((oneMinusT * oneMinusT * oneMinusT) * p1)
                        + (3 * (oneMinusT * oneMinusT) * t * p2)
                        + (3 * oneMinusT * (t * t) * p3)
                        + ((t * t * t) * p4);

                return p;
            }

            /// <summary>
            /// Converts the bezier curve into approximated lines.
            /// </summary>
            /// <param name="n">Number of lines required (minimum is 1).</param>
            /// <returns></returns>
            public IReadOnlyList<Line> ToLines(int n)
            {
                if (n < 1)
                {
                    throw new ArgumentException("BezierCurve.ToLines(): n must be greater than 0.");
                }

                List<Line> lines = new List<Line>();
                var previousPoint = StartPoint;

                for (int p = 1; p <= n; p++)
                {
                    double t = p / (double)n;
                    var currentPoint = new PdfPoint(ValueWithT(StartPoint.X, FirstControlPoint.X, SecondControlPoint.X, EndPoint.X, t),
                                                    ValueWithT(StartPoint.Y, FirstControlPoint.Y, SecondControlPoint.Y, EndPoint.Y, t));
                    lines.Add(new Line(previousPoint, currentPoint));
                    previousPoint = currentPoint;
                }
                return lines;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is BezierCurve curve)
                {
                    return StartPoint.Equals(curve.StartPoint) &&
                           FirstControlPoint.Equals(curve.FirstControlPoint) &&
                           SecondControlPoint.Equals(curve.SecondControlPoint) &&
                           EndPoint.Equals(curve.EndPoint);
                }
                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return (StartPoint, FirstControlPoint, SecondControlPoint, EndPoint).GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="PdfPath"/>s for equality. Paths will only be considered equal if the commands which construct the paths are in the same order.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is PdfPath path)
            {
                if (Commands.Count != path.Commands.Count) return false;

                for (int i = 0; i < Commands.Count; i++)
                {
                    if (!Commands[i].Equals(path.Commands[i])) return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the hash code. Paths will only have the same hash code if the commands which construct the paths are in the same order.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = Commands.Count + 1;
            for (int i = 0; i < Commands.Count; i++)
            {
                hash = hash * (i + 1) * 17 + Commands[i].GetHashCode();
            }
            return hash;
        }
    }
}
