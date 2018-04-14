namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using System;
    using Geometry;

    internal class SimpleGlyphDescription : IGlyphDescription
    {
        /// <summary>
        /// The bounding rectangle for the character.
        /// </summary>
        public PdfRectangle GlyphBounds { get; }

        /// <summary>
        /// The total number of bytes for instructions.
        /// </summary>
        public byte[] Instructions { get; }

        /// <summary>
        /// An array of the last points of each contour.
        /// </summary>
        public int[] EndPointsOfContours { get; }

        public GlyphPoint[] Points { get; set; }

        public SimpleGlyphDescription(byte[] instructions, int[] endPointsOfContours, GlyphPoint[] points,
            PdfRectangle bounds)
        {
            Instructions = instructions;
            EndPointsOfContours = endPointsOfContours;
            Points = points;
            GlyphBounds = bounds;
        }

        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => this;

        public CompositeGlyphDescription CompositeGlyph { get; } = null;

        public IGlyphDescription DeepClone()
        {
            var clonedInstructions = new byte[Instructions.Length];
            Array.Copy(Instructions, clonedInstructions, Instructions.Length);

            var clonedEndPoints = new int[EndPointsOfContours.Length];
            Array.Copy(EndPointsOfContours, clonedEndPoints, EndPointsOfContours.Length);

            var clonedPoints = new GlyphPoint[Points.Length];
            Array.Copy(Points, clonedPoints, Points.Length);

            return new SimpleGlyphDescription(clonedInstructions, clonedEndPoints, clonedPoints, GlyphBounds);
        }
    }
}