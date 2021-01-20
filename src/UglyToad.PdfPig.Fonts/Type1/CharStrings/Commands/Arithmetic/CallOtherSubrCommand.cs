namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Arithmetic
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction;

    /// <summary>
    /// Call other subroutine command. Arguments are pushed onto the PostScript interpreter operand stack then
    /// the PostScript language procedure at the other subroutine index in the OtherSubrs array in the Private dictionary
    /// (or a built-in function equivalent to this procedure) is executed.
    /// </summary>
    internal static class CallOtherSubrCommand
    {
        private const int FlexEnd = 0;
        private const int FlexBegin = 1;
        private const int FlexMiddle = 2;
        private const int HintReplacement = 3;

        public const string Name = "callothersubr";

        public static readonly byte First = 12;
        public static readonly byte? Second = 16;

        public static bool TakeFromStackBottom { get; } = false;
        public static bool ClearsOperandStack { get; } = false;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var index = (int)context.Stack.PopTop();

            // What it should do
            var numberOfArguments = (int)context.Stack.PopTop();
            var otherSubroutineArguments = new List<double>(numberOfArguments);
            for (int j = 0; j < numberOfArguments; j++)
            {
                otherSubroutineArguments.Add(context.Stack.PopTop());
            }

            switch (index)
            {
                // Other subrs 0-2 implement flex
                case FlexEnd:
                    {
                        // https://github.com/apache/pdfbox/blob/2c23d8b4e3ad61852f0b6ee2b95b907eefba1fcf/fontbox/src/main/java/org/apache/fontbox/cff/Type1CharString.java#L339
                        context.IsFlexing = false;
                        if (context.FlexPoints.Count < 7)
                        {
                            throw new NotSupportedException("There must be at least 7 flex points defined by an other subroutine.");
                        }

                        // reference point is relative to start point
                        PdfPoint reference = context.FlexPoints[0];
                        reference = reference.Translate(context.CurrentPosition.X, context.CurrentPosition.Y);

                        // first point is relative to reference point
                        PdfPoint first = context.FlexPoints[1];
                        first = first.Translate(reference.X, reference.Y);

                        // make the first point relative to the start point
                        first = first.Translate(-context.CurrentPosition.X, -context.CurrentPosition.Y);

                        context.Stack.Push(first.X);
                        context.Stack.Push(first.Y);
                        context.Stack.Push(context.FlexPoints[2].X);
                        context.Stack.Push(context.FlexPoints[2].Y);
                        context.Stack.Push(context.FlexPoints[3].X);
                        context.Stack.Push(context.FlexPoints[3].Y);
                        RelativeRCurveToCommand.Run(context);

                        context.Stack.Push(context.FlexPoints[4].X);
                        context.Stack.Push(context.FlexPoints[4].Y);
                        context.Stack.Push(context.FlexPoints[5].X);
                        context.Stack.Push(context.FlexPoints[5].Y);
                        context.Stack.Push(context.FlexPoints[6].X);
                        context.Stack.Push(context.FlexPoints[6].Y);
                        RelativeRCurveToCommand.Run(context);

                        context.ClearFlexPoints();
                        break;
                    }
                case FlexBegin:
                    Debug.Assert(otherSubroutineArguments.Count == 0, "Flex begin should have no arguments.");

                    context.PostscriptStack.Clear();
                    context.PostscriptStack.Push(context.CurrentPosition.X);
                    context.PostscriptStack.Push(context.CurrentPosition.Y);
                    context.IsFlexing = true;
                    break;
                case FlexMiddle:
                    Debug.Assert(otherSubroutineArguments.Count == 0, "Flex middle should have no arguments.");

                    context.PostscriptStack.Push(context.CurrentPosition.X);
                    context.PostscriptStack.Push(context.CurrentPosition.Y);
                    break;
                // Other subrs 3 implements hint replacement
                case HintReplacement:
                    if (otherSubroutineArguments.Count != 1)
                    {
                        throw new InvalidOperationException("The hint replacement subroutine only takes a single argument.");
                    }

                    context.PostscriptStack.Clear();
                    context.PostscriptStack.Push(otherSubroutineArguments[0]);
                    break;
                default:
                    // Other subrs beyond the first 4 can safely be ignored.
                    context.PostscriptStack.Clear();
                    for (var i = 0; i < otherSubroutineArguments.Count; i++)
                    {
                        context.PostscriptStack.Push(otherSubroutineArguments[i]);
                    }
                    break;
            }
        }
    }
}
