namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative rcurveto. Whereas the arguments to the rcurveto operator in the PostScript language are all relative to the current
    /// point, the arguments to rrcurveto are relative to each other. 
    /// Equivalent to: dx1 dy1 (dx1+dx2) (dy1+dy2) (dx1+dx2+dx3) (dy1+dy2+dy3) rcurveto.
    /// </summary>
    internal static class RelativeRCurveToCommand
    {
        public const string Name = "rrcurveto";

        public static readonly byte First = 8;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var dx1 = context.Stack.PopBottom();
            var dy1 = context.Stack.PopBottom();
            var dx2 = context.Stack.PopBottom();
            var dy2 = context.Stack.PopBottom();
            var dx3 = context.Stack.PopBottom();
            var dy3 = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
