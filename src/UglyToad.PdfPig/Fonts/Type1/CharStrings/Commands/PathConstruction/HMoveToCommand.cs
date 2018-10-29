namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative move to for horizontal dimension only.
    /// </summary>
    internal class HMoveToCommand
    {
        public const string Name = "hmoveto";

        public static readonly byte First = 22;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The distance to move in horizontally.
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// Create a new <see cref="HMoveToCommand"/>.
        /// </summary>
        /// <param name="x">The distance to move horizontally.</param>
        public HMoveToCommand(int x)
        {
            X = x;
        }

        public override string ToString()
        {
            return $"{X} {Name}";
        }
    }
}
