namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    using Geometry;

    /// <summary>
    /// Sets left sidebearing and the character width vector.
    /// This command also sets the current point to(sbx, sby), but does not place the point in the character path.
    /// </summary>
    internal class SbwCommand
    {
        public const string Name = "sbw";

        public static readonly byte First = 12;
        public static readonly byte? Second = 7;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var leftSidebearingX = context.Stack.PopBottom();
            var leftSidebearingY = context.Stack.PopBottom();
            var characterWidthX = context.Stack.PopBottom();
            var characterWidthY = context.Stack.PopBottom();

            context.LeftSideBearingX = leftSidebearingX;
            context.LeftSideBearingY = leftSidebearingY;

            context.WidthX = characterWidthX;
            context.WidthY = characterWidthY;

            context.CurrentPosition = new PdfPoint(leftSidebearingX, leftSidebearingY);

            context.Stack.Clear();
        }
    }
}
