namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative line-to command. Creates a line moving a distance relative to the current point.
    /// </summary>
    internal static class RLineToCommand
    {
        public const string Name = "rlineto";

        public static readonly byte First = 5;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var deltaX = context.Stack.PopBottom();
            var deltaY = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
