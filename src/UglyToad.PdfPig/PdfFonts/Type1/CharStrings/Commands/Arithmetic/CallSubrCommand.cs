namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Calls a subroutine with index from the subroutines array in the Private dictionary.
    /// </summary>
    internal static class CallSubrCommand
    {
        public const string Name = "callsubr";

        public static readonly byte First = 10;
        public static readonly byte? Second = null;
        
        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var index = (int)context.Stack.PopTop();

            var subroutine = context.Subroutines[index];

            foreach (var command in subroutine.Commands)
            {
                command.Match(x => context.Stack.Push(x),
                    x => x.Run(context));
            }
        }
    }
}
