namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Closes a sub-path. This command does not reposition the current point.
    /// </summary>
    internal static class ClosePathCommand
    {
        public const string Name = "closepath";

        public static readonly byte First = 9;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);
        
        public static void Run(Type1BuildCharContext context)
        {
            context.Path[context.Path.Count - 1].CloseSubpath();
            context.Stack.Clear();
        }
    }
}
