namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Sets the current point to (x, y) in absolute character space coordinates without performing a charstring moveto command.
    /// </summary>
    internal class SetCurrentPointCommand
    {
        public const string Name = "setcurrentpoint";

        public static readonly byte First = 12;
        public static readonly byte? Second = 33;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The X coordinate in character space.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// The Y coordinate in character space.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Creates a new <see cref="SetCurrentPointCommand"/>.
        /// </summary>
        /// <param name="x">The X coordinate in character space.</param>
        /// <param name="y">The Y coordinate in character space.</param>
        public SetCurrentPointCommand(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"{X} {Y} {Name}";
        }
    }
}
