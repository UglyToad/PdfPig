namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.PathConstruction
{
    using Geometry;

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
            var dy1 = context.Stack.PopBottom();
            var dx2 = context.Stack.PopBottom();
            var dy2 = context.Stack.PopBottom();
            var dx3 = context.Stack.PopBottom();

            var x1 = context.CurrentPosition.X;
            var y1 = context.CurrentPosition.Y + dy1;

            var x2 = x1 + dx2;
            var y2 = y1 + dy2;

            var x3 = x2 + dx3;
            var y3 = y2;

            context.Path.BezierCurveTo(x1, y1, x2, y2, x3, y3);
            context.CurrentPosition = new PdfPoint(x3, y3);

            context.Stack.Clear();
        }
    }
}
