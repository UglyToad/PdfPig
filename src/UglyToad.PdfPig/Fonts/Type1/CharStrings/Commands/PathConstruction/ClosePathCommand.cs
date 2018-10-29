namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Closes a sub-path. This command does not reposition the current point.
    /// </summary>
    internal class ClosePathCommand
    {
        public const string Name = "closepath";

        public static readonly byte First = 9;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = false;
        public bool ClearsOperandStack { get; } = true;

        public static ClosePathCommand Instance { get; } = new ClosePathCommand();

        private ClosePathCommand()
        {
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
