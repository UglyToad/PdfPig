namespace UglyToad.PdfPig.Fonts.TrueType.Tables
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
        public int InstructionLength { get; }

        /// <summary>
        /// An array of the last points of each contour.
        /// </summary>
        public int[] EndPointsOfContours { get; }

        /// <summary>
        /// Array of flags for each coordinate in the outline.
        /// </summary>
        public SimpleGlyphFlags[] Flags { get; }

        /// <summary>
        /// The x-coordinates of the points in this glyph. The first coordinates are relative to the origin (0, 0)
        /// the rest are relative to the previous point.
        /// </summary>
        public short[] XCoordinates { get; }

        /// <summary>
        /// The y-coordinates of the points in this glyph. The first coordinates are relative to the origin (0, 0)
        /// the rest are relative to the previous point.
        /// </summary>
        public short[] YCoordinates { get; }

        public SimpleGlyphDescription(int instructionLength, int[] endPointsOfContours, SimpleGlyphFlags[] flags, short[] xCoordinates, short[] yCoordinates,
            PdfRectangle bounds)
        {
            InstructionLength = instructionLength;
            EndPointsOfContours = endPointsOfContours;
            Flags = flags;
            XCoordinates = xCoordinates;
            YCoordinates = yCoordinates;
            GlyphBounds = bounds;
        }

        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => this;

        public CompositeGlyphDescription CompositeGlyph { get; } = null;

        public IGlyphDescription DeepClone()
        {
            var clonedEndPoints = new int[EndPointsOfContours.Length];
            Array.Copy(EndPointsOfContours, clonedEndPoints, EndPointsOfContours.Length);

            var clonedFlags = new SimpleGlyphFlags[Flags.Length];
            Array.Copy(Flags, clonedFlags, Flags.Length);

            var clonedXCoordinates = new short[XCoordinates.Length];
            Array.Copy(XCoordinates, clonedXCoordinates, XCoordinates.Length);

            var clonedYCoordinates = new short[YCoordinates.Length];
            Array.Copy(YCoordinates, clonedYCoordinates, YCoordinates.Length);

            return new SimpleGlyphDescription(InstructionLength, clonedEndPoints, clonedFlags, clonedXCoordinates, clonedYCoordinates, GlyphBounds);
        }
    }

    internal class CompositeGlyphDescription : IGlyphDescription
    {
        public bool IsSimple { get; } = false;

        public SimpleGlyphDescription SimpleGlyph { get; } = null;

        public CompositeGlyphDescription CompositeGlyph => this;

        public PdfRectangle GlyphBounds { get; }
        public IGlyphDescription DeepClone()
        {
            return new CompositeGlyphDescription();
        }
    }

    internal class EmptyGlyph : IGlyphDescription
    {
        public bool IsSimple { get; } = true;

        public SimpleGlyphDescription SimpleGlyph => new SimpleGlyphDescription(0, new int[0], new SimpleGlyphFlags[0], new short[0], new short[0], GlyphBounds);

        public CompositeGlyphDescription CompositeGlyph { get; } = null;

        public PdfRectangle GlyphBounds { get; }

        public EmptyGlyph(PdfRectangle glyphBounds)
        {
            GlyphBounds = glyphBounds;
        }

        public IGlyphDescription DeepClone()
        {
            return new EmptyGlyph(GlyphBounds);
        }
    }
}