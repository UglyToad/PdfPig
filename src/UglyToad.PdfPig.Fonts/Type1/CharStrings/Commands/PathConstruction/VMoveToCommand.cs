namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

    /// <summary>
    /// Vertical move to. Moves relative to the current point.
    /// </summary>
    internal static class VMoveToCommand
    {
        public const string Name = "vmoveto";

        public static readonly byte First = 4;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var deltaY = context.Stack.PopBottom();

            if (context.IsFlexing)
            {
                // TODO: flex commands
            }
            else
            {
                var y = context.CurrentPosition.Y + deltaY;
                var x = context.CurrentPosition.X;

                context.CurrentPosition = new PdfPoint(x, y);

                context.Path.Add(new PdfSubpath());
                context.Path[context.Path.Count - 1].MoveTo(x, y);
            }

            context.Stack.Clear();
        }
    }
}
