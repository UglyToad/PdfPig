namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Horizontal vertical curve to command. Draws a Bézier curve when the first Bézier tangent is horizontal and the second Bézier tangent is vertical.
    /// Equivalent to dx1 0 dx2 dy2 0 dy3 rrcurveto.
    /// </summary>
    internal static class HvCurveToCommand
    {
        public const string Name = "hvcurveto";

        public static readonly byte First = 31;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var postControlPointDeltaX = context.Stack.PopBottom();
            var preControlPointDeltaX = context.Stack.PopBottom();
            var preControlPointDeltaY = context.Stack.PopBottom();
            var endPointDeltaY = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
