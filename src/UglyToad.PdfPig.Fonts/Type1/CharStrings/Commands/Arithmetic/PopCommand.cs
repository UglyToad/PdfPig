namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Pops a number from the top of the PostScript interpreter operand stack and pushes that number onto the operand stack.
    /// This command is used only to retrieve a result from an OtherSubrs procedure.
    /// </summary>
    internal static class PopCommand
    {
        public const string Name = "pop";

        public static readonly byte First = 12;
        public static readonly byte? Second = 17;
        
        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var num = context.PostscriptStack.PopTop();
            context.Stack.Push(num);
        }
    }
}
