namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative move to command. starts a new subpath of the current path in the same manner as moveto.
    /// However, the number pair is interpreted as a displacement relative to the current point (x, y) rather than as an absolute coordinate.
    /// </summary>
    /// <remarks>
    /// moveto: moveto sets the current point in the graphics state to the user space coordinate (x, y) without adding any line segments to the current path.
    /// If the previous path operation in the current path was also a moveto or rmoveto, 
    /// that point is deleted from the current path and the new moveto point replaces it.
    /// </remarks>
    internal class RMoveToCommand
    {
        public const string Name = "rmoveto";

        public static readonly byte First = 21;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The distance to move horizontally.
        /// </summary>
        public int DeltaX { get; }

        /// <summary>
        /// The distance to move vertically.
        /// </summary>
        public int DeltaY { get; }

        /// <summary>
        /// Create a new <see cref="RMoveToCommand"/>.
        /// </summary>
        /// <param name="deltaX">The distance to move horizontally.</param>
        /// <param name="deltaY">The distance to move vertically.</param>
        public RMoveToCommand(int deltaX, int deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public override string ToString()
        {
            return $"{DeltaX} {DeltaY} {Name}";
        }
    }
}
