namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Vertical-line to command.
    /// </summary>
    internal class VLineToCommand
    {
        public const string Name = "vlineto";

        public static readonly byte First = 7;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The length of the vertical line.
        /// </summary>
        public int DeltaY { get; }

        /// <summary>
        /// Create a new <see cref="VLineToCommand"/>.
        /// </summary>
        /// <param name="deltaY">The length of the vertical line.</param>
        public VLineToCommand(int deltaY)
        {
            DeltaY = deltaY;
        }

        public override string ToString()
        {
            return $"{DeltaY} {Name}";
        }
    }
}
