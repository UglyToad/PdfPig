namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Returns from a charstring subroutine and continues execution in the calling charstring.
    /// </summary>
    internal class ReturnCommand
    {
        public const string Name = "return";

        public static readonly byte First = 11;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = false;

        public static ReturnCommand Instance { get; } = new ReturnCommand();

        private ReturnCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
