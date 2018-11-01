namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    using System.Collections.Generic;

    /// <summary>
    /// Call other subroutine command. Arguments are pushed onto the PostScript interpreter operand stack then
    /// the PostScript language procedure at the other subroutine index in the OtherSubrs array in the Private dictionary
    /// (or a built-in function equivalent to this procedure) is executed.
    /// </summary>
    internal static class CallOtherSubrCommand
    {
        public const string Name = "callothersubr";

        public static readonly byte First = 12;
        public static readonly byte? Second = 16;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;
        
        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var index = (int)context.Stack.PopTop();
            var numberOfArguments = (int)context.Stack.PopTop();
            var otherSubroutineArguments = new List<decimal>(numberOfArguments);
            for (int j = 0; j < numberOfArguments; j++)
            {
                otherSubroutineArguments.Add(context.Stack.PopTop());
            }
        }
    }
}
