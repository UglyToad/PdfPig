namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Calls a subroutine with index from the subroutines array in the Private dictionary.
    /// </summary>
    internal class CallSubrCommand
    {
        public const string Name = "callsubr";

        public static readonly byte First = 10;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = false;

        /// <summary>
        /// The index of the subroutine in the Private dictionary.
        /// </summary>
        public int SubroutineIndex { get; }

        /// <summary>
        /// Creates a new <see cref="CallSubrCommand"/>.
        /// </summary>
        /// <param name="subroutineIndex">The index of the subroutine in the Private dictionary.</param>
        public CallSubrCommand(int subroutineIndex)
        {
            SubroutineIndex = subroutineIndex;
        }

        public override string ToString()
        {
            return $"{SubroutineIndex} {Name}";
        }
    }
}
