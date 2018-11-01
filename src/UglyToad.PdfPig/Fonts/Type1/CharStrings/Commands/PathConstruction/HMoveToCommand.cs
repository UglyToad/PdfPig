namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
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

            context.Stack.Clear();
        }
    }
}
