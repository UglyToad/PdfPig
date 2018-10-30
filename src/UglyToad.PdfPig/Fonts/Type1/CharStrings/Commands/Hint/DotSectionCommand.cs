namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Brackets an outline section for the dots in letters such as "i", "j" and "!".
    /// </summary>
    internal class DotSectionCommand
    {
        public const string Name = "dotsection";

        public static readonly byte First = 12;
        public static readonly byte? Second = 0;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = true;

        public static DotSectionCommand Instance { get; } = new DotSectionCommand();

        private DotSectionCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
