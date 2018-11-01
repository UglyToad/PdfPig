namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
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
        }
    }
}
