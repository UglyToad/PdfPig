namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    using System.Collections.Generic;

    /// <summary>
    /// Call other subroutine command. Arguments are pushed onto the PostScript interpreter operand stack then
    /// the PostScript language procedure at the other subroutine index in the OtherSubrs array in the Private dictionary
    /// (or a built-in function equivalent to this procedure) is executed.
    /// </summary>
    internal class CallOtherSubrCommand
    {
        public const string Name = "callothersubr";

        public static readonly byte First = 12;
        public static readonly byte? Second = 16;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = false;

        /// <summary>
        /// The arguments to pass to the other subroutine.
        /// </summary>
        public IReadOnlyList<int> Arguments { get; }

        /// <summary>
        /// The number of arguments to pass to the other subroutine.
        /// </summary>
        public int ArgumentCount { get; }

        /// <summary>
        /// The index of the other subroutine to call in the font's Private dictionary.
        /// </summary>
        public int OtherSubroutineIndex { get; }

        /// <summary>
        /// Create a new <see cref="CallOtherSubrCommand"/>.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the other subroutine.</param>
        /// <param name="argumentCount">The number of arguments to pass to the other subroutine.</param>
        /// <param name="otherSubroutineIndex">The index of the other subroutine to call in the font's Private dictionary.</param>
        public CallOtherSubrCommand(IReadOnlyList<int> arguments, int argumentCount, int otherSubroutineIndex)
        {
            Arguments = arguments;
            ArgumentCount = argumentCount;
            OtherSubroutineIndex = otherSubroutineIndex;
        }

        public override string ToString()
        {
            var joined = string.Join(" ", Arguments);
            return $"{joined} {ArgumentCount} {OtherSubroutineIndex} {Name}";
        }
    }
}
