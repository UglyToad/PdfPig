namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// This operator returns the result of dividing num1 by num2. The result is always a real.
    /// </summary>
    internal static class DivCommand
    {
        public const string Name = "div";

        public static readonly byte First = 12;
        public static readonly byte? Second = 12;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);
        
        public static void Run(Type1BuildCharContext context)
        {
            var first = context.Stack.PopTop();
            var second = context.Stack.PopTop();

            var result = second / first;

            context.Stack.Push(result);
        }
    }
}
