namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.StartFinishOutline
{
    using Encodings;

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
            var baseCharacterCode = (int)context.Stack.PopBottom();
            var accentCharacterCode = (int)context.Stack.PopBottom();

            // Both bchar and achar are codes that these characters are assigned in the Adobe StandardEncoding vector
            var baseCharacterName = StandardEncoding.Instance.CodeToNameMap[baseCharacterCode];
            var accentCharacterName = StandardEncoding.Instance.CodeToNameMap[accentCharacterCode];

            var baseCharacter = context.GetCharacter(baseCharacterName);
            var accentCharacter = context.GetCharacter(accentCharacterName);

            // TODO: full seac implementation.
            context.SetPath(baseCharacter);

            context.Stack.Clear();
        }
    }
}
