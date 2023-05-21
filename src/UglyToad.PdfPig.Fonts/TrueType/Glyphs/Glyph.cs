namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Core;
    using System;
    using System.Collections.Generic;

    internal class Glyph : IGlyphDescription
    {
        /// <summary>
        /// The bounding rectangle for the character.
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// The bytes of the instructions for this glyph.
        /// </summary>
        public byte[] Instructions { get; }

        /// <summary>
        /// An array of the last points of each contour.
        /// </summary>
        public ushort[] EndPointsOfContours { get; }

        public GlyphPoint[] Points { get; }

        public bool IsSimple { get; }

        public bool IsEmpty => Points.Length == 0;

        public Glyph(bool isSimple, byte[] instructions, ushort[] endPointsOfContours, GlyphPoint[] points,
            PdfRectangle bounds)
        {
            IsSimple = isSimple;
            Instructions = instructions;
            EndPointsOfContours = endPointsOfContours;
            Points = points;
            Bounds = bounds;
        }

        public static IGlyphDescription Empty(PdfRectangle bounds)
        {
            return new Glyph(true, new byte[0], new ushort[0], new GlyphPoint[0], bounds);
        }

        public IGlyphDescription DeepClone()
        {
            var clonedInstructions = new byte[Instructions.Length];
            Array.Copy(Instructions, clonedInstructions, Instructions.Length);

            var clonedEndPoints = new ushort[EndPointsOfContours.Length];
            Array.Copy(EndPointsOfContours, clonedEndPoints, EndPointsOfContours.Length);

            var clonedPoints = new GlyphPoint[Points.Length];
            Array.Copy(Points, clonedPoints, Points.Length);

            return new Glyph(false, clonedInstructions, clonedEndPoints, clonedPoints, Bounds);
        }

        public IGlyphDescription Merge(IGlyphDescription glyph)
        {
            var newPoints = MergePoints(glyph);
            var newEndpoints = MergeContourEndPoints(glyph);

            return new Glyph(false, Instructions, newEndpoints, newPoints, Bounds);
        }

        private GlyphPoint[] MergePoints(IGlyphDescription glyph)
        {
            var newPoints = new GlyphPoint[Points.Length + glyph.Points.Length];

            for (int i = 0; i < Points.Length; i++)
            {
                newPoints[i] = Points[i];
            }

            for (int i = 0; i < glyph.Points.Length; i++)
            {
                newPoints[i + Points.Length] = glyph.Points[i];
            }

            return newPoints;
        }

        private ushort[] MergeContourEndPoints(IGlyphDescription glyph)
        {
            var destinationLastEndPoint = EndPointsOfContours[EndPointsOfContours.Length - 1] + 1;

            var endPoints = new ushort[EndPointsOfContours.Length + glyph.EndPointsOfContours.Length];

            for (var i = 0; i < EndPointsOfContours.Length; i++)
            {
                endPoints[i] = EndPointsOfContours[i];
            }

            for (var i = 0; i < glyph.EndPointsOfContours.Length; i++)
            {
                endPoints[i + EndPointsOfContours.Length] = (ushort)(glyph.EndPointsOfContours[i] + destinationLastEndPoint);
            }

            return endPoints;
        }

        public IGlyphDescription Transform(CompositeTransformMatrix3By2 matrix)
        {
            var newPoints = new GlyphPoint[Points.Length];

            for (var i = Points.Length - 1; i >= 0; i--)
            {
                var point = Points[i];

                var scaled = matrix.ScaleAndRotate(new PdfPoint(point.X, point.Y));

                scaled = matrix.Translate(scaled);

                newPoints[i] = new GlyphPoint((short)scaled.X, (short)scaled.Y, point.IsOnCurve, point.IsEndOfContour);
            }

            return new Glyph(IsSimple, Instructions, EndPointsOfContours, newPoints, Bounds);
        }

        #region Subpaths
        public bool TryGetGlyphPath(out IReadOnlyList<PdfSubpath> subpaths)
        {
            subpaths = EmptyArray<PdfSubpath>.Instance;
            if (Points == null)
            {
                return false;
            }

            if (Points.Length > 0)
            {
                subpaths = CalculatePath(Points);
            }
            return true;
        }

        private static IReadOnlyList<PdfSubpath> CalculatePath(GlyphPoint[] points)
        {
            // https://github.com/apache/pdfbox/blob/trunk/fontbox/src/main/java/org/apache/fontbox/ttf/GlyphRenderer.java
            var path = new List<PdfSubpath>();

            int start = 0;
            for (int p = 0; p < points.Length; ++p)
            {
                if (points[p].IsEndOfContour)
                {
                    PdfSubpath subpath = new PdfSubpath();
                    GlyphPoint firstPoint = points[start];
                    GlyphPoint lastPoint = points[p];
                    var contour = new List<GlyphPoint>();

                    for (int q = start; q <= p; ++q)
                    {
                        contour.Add(points[q]);
                    }

                    if (points[start].IsOnCurve)
                    {
                        // using start point at the contour end
                        contour.Add(firstPoint);
                    }
                    else if (points[p].IsOnCurve)
                    {
                        // first is off-curve point, trying to use one from the end
                        contour.Insert(0, lastPoint);
                    }
                    else
                    {
                        // start and end are off-curve points, creating implicit one
                        var pmid = midValue(firstPoint, lastPoint);
                        contour.Insert(0, pmid);
                        contour.Add(pmid);
                    }

                    subpath.MoveTo(contour[0].X, contour[0].Y);
                    for (int j = 1; j < contour.Count; j++)
                    {
                        GlyphPoint pNow = contour[j];
                        if (pNow.IsOnCurve)
                        {
                            subpath.LineTo(pNow.X, pNow.Y);
                        }
                        else if (contour[j + 1].IsOnCurve)
                        {
                            var pPrevious = contour[j - 1];
                            var pNext = contour[j + 1];
                            subpath.BezierCurveTo(pPrevious.X, pPrevious.Y, pNow.X, pNow.Y, pNext.X, pNext.Y);
                            ++j;
                        }
                        else
                        {
                            var pPrevious = contour[j - 1];
                            var pmid = midValue(pNow, contour[j + 1]);
                            subpath.BezierCurveTo(pPrevious.X, pPrevious.Y, pNow.X, pNow.Y, pmid.X, pmid.Y);
                        }
                    }
                    subpath.CloseSubpath();
                    path.Add(subpath);
                    start = p + 1;
                }
            }

            return path;
        }

        private static short midValue(short a, short b)
        {
            return (short)(a + (b - a) / 2);
        }

        /// <summary>
        /// This creates an onCurve point that is between point1 and point2.
        /// </summary>
        private static GlyphPoint midValue(GlyphPoint point1, GlyphPoint point2)
        {
            // this constructs an on-curve, non-endofcountour point
            return new GlyphPoint(midValue(point1.X, point2.X), midValue(point1.Y, point2.Y), true, false);
        }
        #endregion

        public override string ToString()
        {
            var type = IsSimple ? "S" : "C";

            return $"{type}: Width {Bounds.Width}, Height: {Bounds.Height}, Points: {Points.Length}";
        }
    }
}