namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// Finishes a charstring outline definition and must be the last command in a character's outline
    /// (except for accented characters defined using seac).
    /// </summary>
    internal class EndCharCommand
    {
        public const string Name = "endchar";

        public static readonly byte First = 14;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = true;

        public static EndCharCommand Instance { get; } = new EndCharCommand();

        private EndCharCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
