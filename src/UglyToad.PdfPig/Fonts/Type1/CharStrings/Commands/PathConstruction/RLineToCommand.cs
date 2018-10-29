namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative line-to command. Creates a line moving a distance relative to the current point.
    /// </summary>
    internal class RLineToCommand
    {
        public const string Name = "rlineto";

        public static readonly byte First = 5;
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
        /// Create a new <see cref="RLineToCommand"/>.
        /// </summary>
        /// <param name="deltaX">The distance to move horizontally.</param>
        /// <param name="deltaY">The distance to move vertically.</param>
        public RLineToCommand(int deltaX, int deltaY)
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
