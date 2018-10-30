namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the vertical range of a horizontal stem zone between the y coordinates y and y+dy,
    /// where y is relative to the y coordinate of the left sidebearing point.
    /// </summary>
    internal class HStemCommand
    {
        public const string Name = "hstem";

        public static readonly byte First = 1;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The first Y coordinate for the stem zone, relative to the current left sidebearing Y point.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// The distance to move from Y vertically for the horizontal stem zone.
        /// </summary>
        public int DeltaY { get; }

        /// <summary>
        /// Create a new <see cref="HStemCommand"/>.
        /// </summary>
        /// <param name="y">The lower Y coordinate of the stem zone.</param>
        /// <param name="deltaY">The distance to move from Y vertically for the stem zone.</param>
        public HStemCommand(int y, int deltaY)
        {
            Y = y;
            DeltaY = deltaY;
        }

        public override string ToString()
        {
            return $"{Y} {DeltaY} {Name}";
        }
    }
}
