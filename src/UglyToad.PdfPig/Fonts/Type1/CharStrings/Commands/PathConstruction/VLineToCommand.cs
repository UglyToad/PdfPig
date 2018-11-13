namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Geometry;

    /// <summary>
    /// Vertical-line to command.
    /// </summary>
    internal static class VLineToCommand
    {
        public const string Name = "vlineto";

        public static readonly byte First = 7;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var deltaY = context.Stack.PopBottom();
            var y = context.CurrentPosition.Y + deltaY;

            context.Path.LineTo(context.CurrentPosition.X, y);
            context.CurrentPosition = new PdfPoint(context.CurrentPosition.X, y);

            context.Stack.Clear();
        }
    }
}
