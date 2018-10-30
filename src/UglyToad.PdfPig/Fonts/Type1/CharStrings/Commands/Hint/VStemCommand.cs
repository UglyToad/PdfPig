namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the horizontal range of a vertical stem zone between the x coordinates x and x+dx,
    /// where x is relative to the x coordinate of the left sidebearing point.
    /// </summary>
    internal class VStemCommand
    {
        public const string Name = "vstem";

        public static readonly byte First = 3;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The first X coordinate for the stem zone, relative to the current left sidebearing X point.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// The distance to move from X horizontally for the vertical stem zone.
        /// </summary>
        public int DeltaX { get; }

        public VStemCommand(int x, int deltaX)
        {
            X = x;
            DeltaX = deltaX;
        }

        public override string ToString()
        {
            return $"{X} {DeltaX} {Name}";
        }
    }
}
