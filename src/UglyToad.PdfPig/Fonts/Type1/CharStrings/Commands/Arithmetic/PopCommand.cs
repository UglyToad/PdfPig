namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Pops a number from the top of the interpreter operand stack and pushes that number onto the operand stack.
    /// This command is used only to retrieve a result from an OtherSubrs procedure.
    /// </summary>
    internal class PopCommand
    {
        public const string Name = "pop";

        public static readonly byte First = 12;
        public static readonly byte? Second = 17;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = false;

        public static PopCommand Instance { get; } = new PopCommand();

        private PopCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
