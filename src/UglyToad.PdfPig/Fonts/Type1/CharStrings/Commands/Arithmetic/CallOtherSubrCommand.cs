namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    using System;
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
            var index = (int) context.Stack.PopTop();
            
            // What it should do
            //var numberOfArguments = (int)context.Stack.PopTop();
            //var otherSubroutineArguments = new List<decimal>(numberOfArguments);
            //for (int j = 0; j < numberOfArguments; j++)
            //{
            //    otherSubroutineArguments.Add(context.Stack.PopTop());
            //}

            switch (index)
            {
                case 0:
                {
                    context.IsFlexing = false;
                    if (context.FlexPoints.Count < 7)
                    {
                        throw new NotSupportedException("There must be at least 7 flex points defined by an other subroutine.");
                    }

                    context.ClearFlexPoints();
                    break;
                }
                case 1:
                    context.IsFlexing = true;
                    break;
            }
        }
    }
}
