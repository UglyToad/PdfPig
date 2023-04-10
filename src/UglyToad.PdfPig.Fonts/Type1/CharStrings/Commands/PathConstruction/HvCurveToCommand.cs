namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

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
            var dx1 = context.Stack.PopBottom();
            var dx2 = context.Stack.PopBottom();
            var dy2 = context.Stack.PopBottom();
            var dy3 = context.Stack.PopBottom();

            var x1 = context.CurrentPosition.X + dx1;
            var y1 = context.CurrentPosition.Y;

            var x2 = x1 + dx2;
            var y2 = y1 + dy2;

            var x3 = x2;
            var y3 = y2 + dy3;

            context.Path[context.Path.Count - 1].BezierCurveTo(x1, y1, x2, y2, x3, y3);
            context.CurrentPosition = new PdfPoint(x3, y3);

            context.Stack.Clear();
        }
    }
}
