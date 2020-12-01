namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    using Core;

    /// <summary>
    /// Sets the current point to (x, y) in absolute character space coordinates without performing a charstring moveto command.
    /// <para>This establishes the current point for a subsequent relative path building command.
    /// The 'setcurrentpoint' command is used only in conjunction with results from 'OtherSubrs' procedures.</para>
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

            //context.CurrentPosition = new PdfPoint(x, y);
            // TODO: need to investigate why odd behavior when the current point is actualy set.
            context.Stack.Clear();
        }
    }
}
