namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands.Arithmetic
{
    using Core;

    /// <summary>
    /// Sets the current point to (x, y) in absolute character space coordinates without performing a charstring moveto command.
    /// </summary>
    internal static class SetCurrentPointCommand
    {
        public const string Name = "setcurrentpoint";

        public static readonly byte First = 12;
        public static readonly byte? Second = 33;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var x = context.Stack.PopBottom();
            var y = context.Stack.PopBottom();

            context.CurrentPosition = new PdfPoint(x, y);

            context.Stack.Clear();
        }
    }
}
