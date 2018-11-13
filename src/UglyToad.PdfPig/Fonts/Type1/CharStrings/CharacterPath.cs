namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System.Collections.Generic;
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

        public string ToSvg()
        {
            var builder = new StringBuilder();
            foreach (var pathCommand in commands)
            {
                pathCommand.WriteSvg(builder);
            }

            if (builder[builder.Length - 1] == ' ')
            {
                builder.Remove(builder.Length - 1, 1);
            }

            return builder.ToString();
        }

        private interface IPathCommand
        {
            void WriteSvg(StringBuilder builder);
        }

        private class Close : IPathCommand
        {
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

            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("L {0} {1} ", To.X, To.Y);
            }
        }

        private class BezierCurve : IPathCommand
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

            public void WriteSvg(StringBuilder builder)
            {
                builder.AppendFormat("C {0} {1}, {2} {3}, {4} {5} ", FirstControlPoint.X, FirstControlPoint.Y, SecondControlPoint.X, SecondControlPoint.Y,
                    EndPoint.X, EndPoint.Y);
            }
        }
    }
}
