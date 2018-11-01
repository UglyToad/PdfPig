namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the vertical ranges of three horizontal stem zones:
    /// 1st: between y0 and y0 + dy0
    /// 2nd: between y1 and y1 + dy1
    /// 3rd: between y2 and y2 + dy2
    /// Where y0, y1 and y2 are all relative to the y coordinate of the left sidebearing point.
    /// </summary>
    /// <remarks>
    /// Suited to letters with 3 horizontal stems like 'E'.
    /// </remarks>
    internal static class HStem3Command
    {
        public const string Name = "hstem3";

        public static readonly byte First = 12;
        public static readonly byte? Second = 2;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var y0 = context.Stack.PopBottom();
            var dy0 = context.Stack.PopBottom();
            var y1 = context.Stack.PopBottom();
            var dy1 = context.Stack.PopBottom();
            var y2 = context.Stack.PopBottom();
            var dy2 = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
