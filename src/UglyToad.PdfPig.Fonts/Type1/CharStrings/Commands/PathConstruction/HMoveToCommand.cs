namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

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
            var deltaX = context.Stack.PopBottom();

            if (context.IsFlexing)
            {
                // not in the Type 1 spec, but exists in some fonts
                context.AddFlexPoint(new PdfPoint(deltaX, 0));
            }
            else
            {
                var x = context.CurrentPosition.X + deltaX;
                var y = context.CurrentPosition.Y;
                context.CurrentPosition = new PdfPoint(x, y);
                context.Path.MoveTo(x, y);
            }

            context.Stack.Clear();
        }
    }
}
