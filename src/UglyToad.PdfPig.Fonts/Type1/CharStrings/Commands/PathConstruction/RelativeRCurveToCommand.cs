namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

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

            var x1 = context.CurrentPosition.X + dx1;
            var y1 = context.CurrentPosition.Y + dy1;

            var x2 = x1 + dx2;
            var y2 = y1 + dy2;

            var x3 = x2 + dx3;
            var y3 = y2 + dy3;

            context.Path[context.Path.Count - 1].BezierCurveTo(x1, y1, x2, y2, x3, y3);

            context.CurrentPosition = new PdfPoint(x3, y3);

            context.Stack.Clear();
        }
    }
}
