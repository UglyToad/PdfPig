namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// Finishes a charstring outline definition and must be the last command in a character's outline
    /// (except for accented characters defined using seac).
    /// </summary>
    internal static class EndCharCommand
    {
        public const string Name = "endchar";

        public static readonly byte First = 14;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            context.Stack.Clear();
        }
    }
}
