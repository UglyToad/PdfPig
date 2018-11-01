namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// Standard encoding accented character.
    /// Makes an accented character from two other characters in the font program.
    /// </summary>
    internal static class SeacCommand
    {
        public const string Name = "seac";

        public static readonly byte First = 12;
        public static readonly byte? Second = 6;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var accentLeftSidebearingX = context.Stack.PopBottom();
            var accentOriginX = context.Stack.PopBottom();
            var accentOriginY = context.Stack.PopBottom();
            var baseCharacterCode = context.Stack.PopBottom();
            var accentCharacterCode = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
