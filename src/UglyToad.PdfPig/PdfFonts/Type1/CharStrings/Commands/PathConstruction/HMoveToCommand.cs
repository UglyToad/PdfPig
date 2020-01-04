namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.PathConstruction
{
    using Geometry;

    /// <summary>
    /// Relative move to for horizontal dimension only.
    /// </summary>
    internal static class HMoveToCommand
    {
        public const string Name = "hmoveto";

        public static readonly byte First = 22;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var x = context.Stack.PopBottom();

            var actualX = context.CurrentPosition.X + x;
            var y = context.CurrentPosition.Y;

            if (context.IsFlexing)
            {
                // TODO: flex support
            }
            else
            {
                context.CurrentPosition = new PdfPoint(actualX, y);
                context.Path.MoveTo(actualX, y);
            }

            context.Stack.Clear();
        }
    }
}
