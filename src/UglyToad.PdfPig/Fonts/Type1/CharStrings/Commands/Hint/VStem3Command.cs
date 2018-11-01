namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the horizontal ranges of three vertical stem zones.
    /// 1st: between x0 and x0 + dx0
    /// 2nd: between x1 and x1 + dx1
    /// 3rd: between x2 and x2 + dx2
    /// Where x0, x1 and x2 are all relative to the x coordinate of the left sidebearing point.
    /// </summary>
    /// <remarks>
    /// Suited to letters with 3 vertical stems, for instance 'm'.
    /// </remarks>
    internal class VStem3Command
    {
        public const string Name = "vstem3";

        public static readonly byte First = 12;
        public static readonly byte? Second = 1;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var x0 = context.Stack.PopBottom();
            var dx0 = context.Stack.PopBottom();
            var x1 = context.Stack.PopBottom();
            var dx1 = context.Stack.PopBottom();
            var x2 = context.Stack.PopBottom();
            var dx2 = context.Stack.PopBottom();

            context.Stack.Clear();
        }
    }
}
