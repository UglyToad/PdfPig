namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Vertical-horizontal curveto. 
    /// Equivalent to 0 dy1 dx2 dy2 dx3 0 rrcurveto.
    /// This command eliminates two arguments from an rrcurveto call when the first Bézier tangent is vertical and the second Bézier tangent is horizontal.
    /// </summary>
    internal static class VhCurveToCommand
    {
        public const string Name = "vhcurveto";

        public static readonly byte First = 30;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var postControlPointDeltaY = context.Stack.PopBottom();
            var preControlPointDeltaX = context.Stack.PopBottom();
            var preControlPointDeltaY = context.Stack.PopBottom();
            var endPointDeltaX = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
