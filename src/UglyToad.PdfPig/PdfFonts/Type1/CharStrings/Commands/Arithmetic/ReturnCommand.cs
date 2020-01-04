namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.Arithmetic
{
    /// <summary>
    /// Returns from a charstring subroutine and continues execution in the calling charstring.
    /// </summary>
    internal static class ReturnCommand
    {
        public const string Name = "return";

        public static readonly byte First = 11;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            // Do nothing
        }
    }
}
