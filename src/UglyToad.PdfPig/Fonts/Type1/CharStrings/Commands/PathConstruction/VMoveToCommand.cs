namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
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

            context.Stack.Clear();
        }
    }
}
