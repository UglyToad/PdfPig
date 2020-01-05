// ReSharper disable UnusedVariable
namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the vertical range of a horizontal stem zone between the y coordinates y and y+dy,
    /// where y is relative to the y coordinate of the left sidebearing point.
    /// </summary>
    internal static class HStemCommand
    {
        public const string Name = "hstem";

        public static readonly byte First = 1;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var y = context.Stack.PopBottom();
            var dy = context.Stack.PopBottom();

            // Ignored

            context.Stack.Clear();
        }
    }
}
