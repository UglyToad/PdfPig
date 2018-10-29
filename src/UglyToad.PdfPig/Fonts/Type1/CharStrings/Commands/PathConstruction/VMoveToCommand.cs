namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Vertical move to. Moves relative to the current point.
    /// </summary>
    internal class VMoveToCommand
    {
        public const string Name = "vmoveto";

        public static readonly byte First = 4;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The distance to move vertically.
        /// </summary>
        public int DeltaY { get; }

        /// <summary>
        /// Create a new <see cref="VMoveToCommand"/>.
        /// </summary>
        /// <param name="deltaY">The distance to move vertically.</param>
        public VMoveToCommand(int deltaY)
        {
            DeltaY = deltaY;
        }

        public override string ToString()
        {
            return $"{DeltaY} {Name}";
        }
    }
}
