namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// This operator returns the result of dividing num1 by num2. The result is always a real.
    /// </summary>
    internal class DivCommand
    {
        public const string Name = "div";

        public static readonly byte First = 12;
        public static readonly byte? Second = 12;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = false;

        public static DivCommand Instance { get;  } = new DivCommand();

        private DivCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
