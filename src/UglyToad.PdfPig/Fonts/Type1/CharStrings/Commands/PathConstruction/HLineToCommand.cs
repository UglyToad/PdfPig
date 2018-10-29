namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Horizontal line-to command.
    /// </summary>
    internal class HLineToCommand
    {
        public const string Name = "hlineto";

        public static readonly byte First = 6;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The length of the horizontal line.
        /// </summary>
        public int DeltaX { get; }

        /// <summary>
        /// Create a new <see cref="HLineToCommand"/>.
        /// </summary>
        /// <param name="deltaX">The length of the horizontal line.</param>
        public HLineToCommand(int deltaX)
        {
            DeltaX = deltaX;
        }

        public override string ToString()
        {
            return $"{DeltaX} {Name}";
        }
    }
}
