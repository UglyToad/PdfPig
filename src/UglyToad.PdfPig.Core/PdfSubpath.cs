namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A supbpath is made up of a sequence of connected segments.
    /// </summary>
    public sealed class PdfSubpath
    {
        private readonly List<IPathCommand> commands = new List<IPathCommand>();

        /// <summary>
        /// The sequence of commands which form this <see cref="PdfSubpath"/>.
        /// </summary>
        public IReadOnlyList<IPathCommand> Commands => commands;

        /// <summary>
        /// True if the <see cref="PdfSubpath"/> was originaly drawn using the rectangle ('re') operator.
        /// <para>Always false if paths are clipped.</para>
        /// </summary>
        public bool IsDrawnAsRectangle { get; internal set; }

        private PdfPoint? currentPosition;

        private double shoeLaceSum;

        /// <summary>
        /// Return true if points are organised in a clockwise order. Works only with closed paths.
        /// </summary>
        public bool IsClockwise => IsClosed() && shoeLaceSum > 0;

        /// <summary>
        /// Return true if points are organised in a counterclockwise order. Works only with closed paths.
        /// </summary>
        public bool IsCounterClockwise => IsClosed() && shoeLaceSum < 0;

        /// <summary>
        /// Get the <see cref="PdfSubpath"/>'s centroid point.
        /// </summary>
        public PdfPoint GetCentroid()
        {
            var filtered = commands.Where(c => c is Line || c is BezierCurve).ToList();
            if (filtered.Count == 0)
            {
                return new PdfPoint();
            }

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
        /// Simplify this <see cref="PdfSubpath"/> by converting everything to <see cref="PdfLine"/>s.
        /// </summary>
        /// <param name="n">Number of lines required (minimum is 1).</param>
        internal PdfSubpath Simplify(int n = 4)
        {
            PdfSubpath simplifiedPath = new PdfSubpath();
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
                // PDF Reference 1.7 p226
                throw new ArgumentNullException("LineTo(): currentPosition is null.");
            }
        }

        /// <summary>
        /// Add a rectangle following the pdf specification (m, l, l, l, c) path. A new subpath is created.
        /// </summary>
        public void Rectangle(double x, double y, double width, double height)
        {
                                                // is equivalent to:
            MoveTo(x, y);                       // x y m
            LineTo(x + width, y);               // (x + width) y l
            LineTo(x + width, y + height);      // (x + width) (y + height) l
            LineTo(x, y + height);              // x (y + height) l
            CloseSubpath();                     // h
            IsDrawnAsRectangle = true;
        }

        /// <summary>
        /// Add a <see cref="CubicBezierCurve"/> to the path.
        /// </summary>
        public void BezierCurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (currentPosition.HasValue)
            {
                shoeLaceSum += (x1 - currentPosition.Value.X) * (y1 + currentPosition.Value.Y);
                shoeLaceSum += (x2 - x1) * (y2 + y1);
                shoeLaceSum += (x3 - x2) * (y3 + y2);

                var to = new PdfPoint(x3, y3);
                commands.Add(new CubicBezierCurve(currentPosition.Value, new PdfPoint(x1, y1), new PdfPoint(x2, y2), to));
                currentPosition = to;
            }
            else
            {
                // PDF Reference 1.7 p226
                throw new ArgumentNullException("BezierCurveTo(): currentPosition is null.");
            }
        }

        /// <summary>
        /// Add a <see cref="QuadraticBezierCurve"/> to the path.
        /// <para>Only used in fonts.</para>
        /// </summary>
        public void BezierCurveTo(double x1, double y1, double x2, double y2)
        {
            if (currentPosition.HasValue)
            {
                shoeLaceSum += (x1 - currentPosition.Value.X) * (y1 + currentPosition.Value.Y);
                shoeLaceSum += (x2 - x1) * (y2 + y1);

                var to = new PdfPoint(x2, y2);
                commands.Add(new QuadraticBezierCurve(currentPosition.Value, new PdfPoint(x1, y1), to));
                currentPosition = to;
            }
            else
            {
                // PDF Reference 1.7 p226
                throw new ArgumentNullException("BezierCurveTo(): currentPosition is null.");
            }
        }

        /// <summary>
        /// Close the path.
        /// </summary>
        public void CloseSubpath()
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
        /// Determines if the path is currently closed.
        /// </summary>
        public bool IsClosed()
        {
            var filteredCount = 0;
            IPathCommand? last = null;
            IPathCommand? first = null;
            for (int i = Commands.Count - 1; i >= 0; i--)
            {
                var cmd = Commands[i];

                if (cmd is Close)
                {
                    return true;
                }

                if (cmd is Line || cmd is BezierCurve || cmd is Move)
                {
                    if (last == null)
                    {
                        last = cmd;
                    }

                    first = cmd;
                    filteredCount++;
                }
            }

            if (filteredCount < 2 || last == null || first == null)
            {
                return false;
            }

            if (!GetStartPoint(first).Equals(GetEndPoint(last)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a <see cref="PdfRectangle"/> which entirely contains the geometry of the defined subpath.
        /// </summary>
        /// <returns>For subpaths which don't define any geometry this returns <see langword="null"/>.</returns>
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
        /// If <see cref="IsDrawnAsRectangle"/> then returns the rectangle dimensions specified. Otherwise returns <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Since a rectangle is interpreted as a move command followed by 3 lines and a close command this condenses the 5 commands back into a single rectangle.
        /// </remarks>
        public PdfRectangle? GetDrawnRectangle()
        {
            if (!IsDrawnAsRectangle || Commands.Count != 5)
            {
                return null;
            }

            if (!(Commands[0] is Move mv) || !(Commands[1] is Line line1) || !(Commands[2] is Line line2) || !(Commands[3] is Line line3)
                || !(Commands[4] is Close))
            {
                return null;
            }

            if (!line1.From.Equals(mv.Location) || line1.To.Y != mv.Location.Y)
            {
                return null;
            }

            var width = line1.To.X - mv.Location.X;

            if (!line2.From.Equals(line1.To) || line2.To.X != line1.To.X)
            {
                return null;
            }

            var height = line2.To.Y - line1.To.Y;

            return new PdfRectangle(mv.Location, new PdfPoint(mv.Location.X + width, mv.Location.Y + height));
        }

        /// <summary>
        /// Gets a <see cref="PdfRectangle"/> which entirely contains the geometry of the defined path.
        /// </summary>
        /// <returns>For paths which don't define any geometry this returns <see langword="null"/>.</returns>
        public static PdfRectangle? GetBoundingRectangle(IReadOnlyList<PdfSubpath>? path)
        {
            if (path == null || path.Count == 0)
            {
                return null;
            }

            var bboxes = path.Select(x => x.GetBoundingRectangle()).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            if (bboxes.Count == 0)
            {
                return null;
            }

            var minX = bboxes.Min(x => x.Left);
            var minY = bboxes.Min(x => x.Bottom);
            var maxX = bboxes.Max(x => x.Right);
            var maxY = bboxes.Max(x => x.Top);
            return new PdfRectangle(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// A command in a <see cref="PdfSubpath"/>.
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
            void WriteSvg(StringBuilder builder, double height);
        }

        /// <summary>
        /// Close the current <see cref="PdfSubpath"/>.
        /// </summary>
        public sealed class Close : IPathCommand
        {
            private static readonly int _hash = typeof(Close).GetHashCode();

            /// <inheritdoc />
            public PdfRectangle? GetBoundingRectangle()
            {
                return null;
            }

            /// <inheritdoc />
            public void WriteSvg(StringBuilder builder, double height)
            {
                builder.Append("Z ");
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                return (obj is Close);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return _hash;
            }
        }

        /// <summary>
        /// Move drawing of the current <see cref="PdfSubpath"/> to the specified location.
        /// </summary>
        public sealed class Move : IPathCommand
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
            public void WriteSvg(StringBuilder builder, double height)
            {
                builder.Append($"M {Location.X} {height - Location.Y} ");
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
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
        public sealed class Line : IPathCommand
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
            /// Length of the line.
            /// </summary>
            public double Length
            {
                get
                {
                    var dx = From.X - To.X;
                    var dy = From.Y - To.Y;
                    return Math.Sqrt(dx * dx + dy * dy);
                }
            }

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
            public void WriteSvg(StringBuilder builder, double height)
            {
                builder.Append($"L {To.X} {height - To.Y} ");
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
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
                return HashCode.Combine(From, To);
            }
        }

        /// <summary>
        /// Draw a quadratic Bezier-curve given by the start, control and end points.
        /// <para>Only used in fonts.</para>
        /// </summary>
        public sealed class QuadraticBezierCurve : BezierCurve
        {
            /// <summary>
            /// Create a quadratic Bezier-curve at the provided points.
            /// <para>Only used in fonts.</para>
            /// </summary>
            public QuadraticBezierCurve(PdfPoint startPoint, PdfPoint controlPoint, PdfPoint endPoint)
                : base(startPoint, endPoint)
            {
                ControlPoint = controlPoint;
            }

            /// <summary>
            /// The control point of the curve.
            /// </summary>
            public PdfPoint ControlPoint { get; }

            /// <inheritdoc/>
            public override void WriteSvg(StringBuilder builder, double height)
            {
                builder.Append($"C {ControlPoint.X} {height - ControlPoint.Y}, {EndPoint.X} {height - EndPoint.Y} ");
            }

            /// <inheritdoc/>
            protected internal override bool TrySolve(bool isX, double currentMin, double currentMax, out (double min, double max) solutions)
            {
                solutions = default((double, double));

                // Given k points the general form is:
                // P = (1-t)^(k - i - 1)*t^(i)*P_i
                // 
                // For 3 points this gives:
                // P = (1-t)^2*P_1 + 2(1-t)*t*P_2 + t^2*P_3
                // The differential is:
                // P' = 2(1-t)(P_2 - P_1) + 2t(P_3 - P_2)

                var p1 = isX ? StartPoint.X : StartPoint.Y;
                var p2 = isX ? ControlPoint.X : ControlPoint.Y;
                var p3 = isX ? EndPoint.X : EndPoint.Y;

                var t = (p1 - p2) / (p1 - 2.0 * p2 + p3);

                if (t >= 0 && t <= 1)
                {
                    var sol = ValueWithT(p1, p2, p3, t);
                    if (sol < currentMin)
                    {
                        currentMin = sol;
                    }

                    if (sol > currentMax)
                    {
                        currentMax = sol;
                    }
                }

                solutions = (currentMin, currentMax);

                return true;
            }

            /// <inheritdoc />
            public override IReadOnlyList<Line> ToLines(int n)
            {
                if (n < 1)
                {
                    throw new ArgumentException("BezierCurve.ToLines(): n must be greater than 0.");
                }

                var lines = new Line[n];
                var previousPoint = StartPoint;

                for (int p = 1; p <= n; p++)
                {
                    double t = p / (double)n;
                    var currentPoint = new PdfPoint(ValueWithT(StartPoint.X, ControlPoint.X, EndPoint.X, t),
                        ValueWithT(StartPoint.Y, ControlPoint.Y, EndPoint.Y, t));
                    lines[p - 1] = new Line(previousPoint, currentPoint);
                    previousPoint = currentPoint;
                }

                return lines;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                if (obj is QuadraticBezierCurve curve)
                {
                    return StartPoint.Equals(curve.StartPoint) &&
                           ControlPoint.Equals(curve.ControlPoint) &&
                           EndPoint.Equals(curve.EndPoint);
                }

                return false;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return HashCode.Combine(StartPoint, ControlPoint, EndPoint);
            }
        }

        /// <summary>
        /// Draw a cubic Bezier-curve given by the start, control and end points.
        /// </summary>
        public sealed class CubicBezierCurve : BezierCurve
        {
            /// <summary>
            /// Create a cubic Bezier-curve at the provided points.
            /// </summary>
            public CubicBezierCurve(PdfPoint startPoint, PdfPoint firstControlPoint, PdfPoint secondControlPoint, PdfPoint endPoint)
                : base(startPoint, endPoint)
            {
                FirstControlPoint = firstControlPoint;
                SecondControlPoint = secondControlPoint;
            }

            /// <summary>
            /// The first control point of the curve.
            /// </summary>
            public PdfPoint FirstControlPoint { get; }

            /// <summary>
            /// The second control point of the curve.
            /// </summary>
            public PdfPoint SecondControlPoint { get; }

            /// <inheritdoc/>
            public override void WriteSvg(StringBuilder builder, double height)
            {
                builder.Append($"C {FirstControlPoint.X} {height - FirstControlPoint.Y}, {SecondControlPoint.X} {height - SecondControlPoint.Y}, {EndPoint.X} {height - EndPoint.Y} ");
            }

            /// <inheritdoc/>
            protected internal override bool TrySolve(bool isX, double currentMin, double currentMax, out (double min, double max) solutions)
            {
                return TrySolveQuadratic(isX, currentMin, currentMax, out solutions);
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

            /// <inheritdoc />
            public override IReadOnlyList<Line> ToLines(int n)
            {
                if (n < 1)
                {
                    throw new ArgumentException("BezierCurve.ToLines(): n must be greater than 0.");
                }

                var lines = new Line[n];
                var previousPoint = StartPoint;

                for (int p = 1; p <= n; p++)
                {
                    double t = p / (double)n;
                    var currentPoint = new PdfPoint(ValueWithT(StartPoint.X, FirstControlPoint.X, SecondControlPoint.X, EndPoint.X, t),
                                                    ValueWithT(StartPoint.Y, FirstControlPoint.Y, SecondControlPoint.Y, EndPoint.Y, t));
                    lines[p - 1] = new Line(previousPoint, currentPoint);
                    previousPoint = currentPoint;
                }
                return lines;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                if (obj is CubicBezierCurve curve)
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
                return HashCode.Combine(StartPoint, FirstControlPoint, SecondControlPoint, EndPoint);
            }
        }

        /// <summary>
        /// Draw a Bezier-curve given by the start, control and end points.
        /// </summary>
        public abstract class BezierCurve : IPathCommand
        {
            /// <summary>
            /// The start point of the Bezier-curve.
            /// </summary>
            public PdfPoint StartPoint { get; }

            /// <summary>
            /// The end point of the curve.
            /// </summary>
            public PdfPoint EndPoint { get; }

            /// <summary>
            /// Create a Bezier-curve at the provided points.
            /// </summary>
            protected BezierCurve(PdfPoint startPoint, PdfPoint endPoint)
            {
                StartPoint = startPoint;
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

                if (TrySolve(true, minX, maxX, out var xSolutions))
                {
                    minX = xSolutions.min;
                    maxX = xSolutions.max;
                }

                if (TrySolve(false, minY, maxY, out var ySolutions))
                {
                    minY = ySolutions.min;
                    maxY = ySolutions.max;
                }

                return new PdfRectangle(minX, minY, maxX, maxY);
            }

            /// <inheritdoc />
            public abstract void WriteSvg(StringBuilder builder, double height);

            /// <summary>
            /// TODO
            /// </summary>
            protected internal abstract bool TrySolve(bool isX, double currentMin, double currentMax, out (double min, double max) solutions);

            /// <summary>
            /// Calculate the value of the Quadratic Bezier-curve at t.
            /// </summary>
            public static double ValueWithT(double p1, double p2, double p3, double t)
            {
                // P = (1−t)^2*P_1 + 2(1−t)*t*P_2 + t^2*P_3
                var oneMinusT = 1 - t;
                var p = ((oneMinusT * oneMinusT) * p1)
                        + (2 * (oneMinusT) * t * p2)
                        + ((t * t) * p3);

                return p;
            }

            /// <summary>
            /// Calculate the value of the Cubic Bezier-curve at t.
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
            public abstract IReadOnlyList<Line> ToLines(int n);
        }

        /// <summary>
        /// Compares two <see cref="PdfSubpath"/>s for equality. Paths will only be considered equal if the commands which construct the paths are in the same order.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (!(obj is PdfSubpath path) || Commands.Count != path.Commands.Count)
            {
                return false;
            }

            for (int i = 0; i < Commands.Count; i++)
            {
                if (!Commands[i].Equals(path.Commands[i]))
                {
                    return false;
                }
            }

            return true;
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
