namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the horizontal range of a vertical stem zone between the x coordinates x and x+dx,
    /// where x is relative to the x coordinate of the left sidebearing point.
    /// </summary>
    internal static class VStemCommand
    {
        public const string Name = "vstem";

        public static readonly byte First = 3;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var x = context.Stack.PopBottom();
            var dx = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
