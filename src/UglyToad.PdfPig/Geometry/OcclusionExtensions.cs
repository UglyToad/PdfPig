namespace UglyToad.PdfPig.Geometry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ClipperLibrary;
    using Core;
    using Graphics;
    using Graphics.Core;
    using static Core.PdfSubpath;

    /// <summary>
    /// Polygon helpers for working out which letters are hidden under later paint.
    /// </summary>
    internal static class OcclusionExtensions
    {
        private const int LinesInCurve = 10;

        /// <summary>
        /// One polygon per subpath, to be combined with the path's filling rule.
        /// </summary>
        public static List<List<ClipperIntPoint>> GetFillPolygons(this PdfPath path)
        {
            var polygons = new List<List<ClipperIntPoint>>();
            foreach (var subpath in path)
            {
                if (subpath.Commands.Count > 1 && subpath.Commands[0] is Move)
                {
                    polygons.Add(subpath.ToClipperPolygon().ToList());
                }
            }

            return polygons;
        }

        public static ClipperPolyFillType GetClipperFillType(this PdfPath path)
        {
            return path.FillingRule == FillingRule.NonZeroWinding ? ClipperPolyFillType.NonZero : ClipperPolyFillType.EvenOdd;
        }

        /// <summary>
        /// Part of the area painted by stroking the path: one rectangle per segment, plus projecting caps, to be combined with nonzero winding.
        /// Joins, round caps and dots only add to the painted area, so leaving them out never overstates it.
        /// </summary>
        public static List<List<ClipperIntPoint>> GetStrokePolygons(this PdfPath path, double lineWidth, LineCapStyle capStyle)
        {
            var polygons = new List<List<ClipperIntPoint>>();
            if (lineWidth <= 0)
            {
                return polygons;
            }

            var halfWidth = lineWidth / 2;
            foreach (var subpath in path)
            {
                var segments = GetSegments(subpath);
                var isOpen = !subpath.IsClosed();
                for (var i = 0; i < segments.Count; i++)
                {
                    var (from, to) = segments[i];
                    var length = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
                    if (length == 0)
                    {
                        continue;
                    }

                    var ux = (to.X - from.X) / length;
                    var uy = (to.Y - from.Y) / length;
                    var nx = -uy * halfWidth;
                    var ny = ux * halfWidth;

                    var square = capStyle == LineCapStyle.ProjectingSquare && isOpen;
                    var start = square && i == 0 ? halfWidth : 0;
                    var end = square && i == segments.Count - 1 ? halfWidth : 0;

                    var p = new PdfPoint(from.X - ux * start, from.Y - uy * start);
                    var q = new PdfPoint(to.X + ux * end, to.Y + uy * end);

                    polygons.Add(new List<ClipperIntPoint>
                    {
                        new PdfPoint(p.X + nx, p.Y + ny).ToClipperIntPoint(),
                        new PdfPoint(q.X + nx, q.Y + ny).ToClipperIntPoint(),
                        new PdfPoint(q.X - nx, q.Y - ny).ToClipperIntPoint(),
                        new PdfPoint(p.X - nx, p.Y - ny).ToClipperIntPoint()
                    });
                }
            }

            return polygons;
        }

        /// <summary>
        /// The smallest factor by which the matrix scales a length, so a transformed line width is never overstated.
        /// </summary>
        public static double GetMinimumScale(this TransformationMatrix matrix)
        {
            var sum = matrix.A * matrix.A + matrix.B * matrix.B + matrix.C * matrix.C + matrix.D * matrix.D;
            var determinant = matrix.A * matrix.D - matrix.B * matrix.C;
            var root = Math.Sqrt(Math.Max(0, sum * sum - 4 * determinant * determinant));
            return Math.Sqrt(Math.Max(0, (sum - root) / 2));
        }

        public static List<List<ClipperIntPoint>> Union(List<List<ClipperIntPoint>> polygons, ClipperPolyFillType fillType)
        {
            return Combine(ClipperClipType.Union, polygons, null, fillType, fillType);
        }

        public static List<List<ClipperIntPoint>> Union(List<List<ClipperIntPoint>> a, List<List<ClipperIntPoint>> b)
        {
            if (a.Count == 0)
            {
                return b;
            }

            return Combine(ClipperClipType.Union, a, b, ClipperPolyFillType.NonZero, ClipperPolyFillType.NonZero);
        }

        public static List<List<ClipperIntPoint>> Intersect(List<List<ClipperIntPoint>> a, List<List<ClipperIntPoint>> b, ClipperPolyFillType bFillType = ClipperPolyFillType.NonZero)
        {
            return Combine(ClipperClipType.Intersection, a, b, ClipperPolyFillType.NonZero, bFillType);
        }

        /// <summary>
        /// The area of polygons as returned by Clipper, where holes have the opposite sign to outlines.
        /// </summary>
        public static double Area(List<List<ClipperIntPoint>> polygons)
        {
            return Math.Abs(polygons.Sum(Clipper.Area));
        }

        public static PdfRectangle? GetBounds(List<ClipperIntPoint> polygon)
        {
            if (polygon.Count == 0)
            {
                return null;
            }

            long minX = long.MaxValue, minY = long.MaxValue, maxX = long.MinValue, maxY = long.MinValue;
            foreach (var point in polygon)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            return new PdfRectangle(minX / ClippingExtensions.Factor, minY / ClippingExtensions.Factor,
                maxX / ClippingExtensions.Factor, maxY / ClippingExtensions.Factor);
        }

        private static List<List<ClipperIntPoint>> Combine(ClipperClipType clipType,
            List<List<ClipperIntPoint>> subject,
            List<List<ClipperIntPoint>>? clip,
            ClipperPolyFillType subjectFillType,
            ClipperPolyFillType clipFillType)
        {
            var solution = new List<List<ClipperIntPoint>>();
            if (subject.Count == 0)
            {
                return solution;
            }

            var clipper = new Clipper();
            clipper.AddPaths(subject, ClipperPolyType.Subject, true);
            if (clip != null)
            {
                clipper.AddPaths(clip, ClipperPolyType.Clip, true);
            }

            clipper.Execute(clipType, solution, subjectFillType, clipFillType);
            return solution;
        }

        private static List<(PdfPoint From, PdfPoint To)> GetSegments(PdfSubpath subpath)
        {
            var segments = new List<(PdfPoint, PdfPoint)>();
            PdfPoint? start = null;
            PdfPoint? current = null;
            foreach (var command in subpath.Commands)
            {
                switch (command)
                {
                    case Move move:
                        start = move.Location;
                        current = move.Location;
                        break;
                    case Line line:
                        segments.Add((line.From, line.To));
                        current = line.To;
                        break;
                    case BezierCurve curve:
                        segments.AddRange(curve.ToLines(LinesInCurve).Select(l => (l.From, l.To)));
                        current = curve.EndPoint;
                        break;
                    case Close when start.HasValue && current.HasValue:
                        segments.Add((current.Value, start.Value));
                        current = start;
                        break;
                }
            }

            return segments;
        }
    }
}
