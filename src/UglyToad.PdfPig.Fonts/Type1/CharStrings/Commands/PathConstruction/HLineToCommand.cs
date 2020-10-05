namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

    /// <summary>
    /// Horizontal line-to command.
    /// </summary>
    internal static class HLineToCommand
    {
        public const string Name = "hlineto";

        public static readonly byte First = 6;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var deltaX = context.Stack.PopBottom();
            var x = context.CurrentPosition.X + deltaX;

            context.Path[context.Path.Count - 1].LineTo(x, context.CurrentPosition.Y);
            context.CurrentPosition = new PdfPoint(x, context.CurrentPosition.Y);

            context.Stack.Clear();
        }
    }
}
