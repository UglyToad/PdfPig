namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using System;
    using Geometry;

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

        public IGlyphDescription Transform(PdfMatrix3By2 matrix)
        {
            var newPoints = new GlyphPoint[Points.Length];

            for (var i = Points.Length - 1; i >= 0; i--)
            {
                var point = Points[i];

                var scaled = matrix.ScaleAndRotate(new PdfPoint(point.X, point.Y));

                scaled = matrix.Translate(scaled);

                newPoints[i] = new GlyphPoint((short)scaled.X, (short)scaled.Y, point.IsOnCurve);
            }

            return new Glyph(IsSimple, Instructions, EndPointsOfContours, newPoints, Bounds);
        }

        public override string ToString()
        {
            var type = IsSimple ? "S" : "C";

            return $"{type}: Width {Bounds.Width}, Height: {Bounds.Height}, Points: {Points.Length}";
        }
    }
}