namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Brackets an outline section for the dots in letters such as "i", "j" and "!".
    /// </summary>
    internal static class DotSectionCommand
    {
        public const string Name = "dotsection";

        public static readonly byte First = 12;
        public static readonly byte? Second = 0;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = true;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            // Ignored.
            context.Stack.Clear();
        }
    }
}
