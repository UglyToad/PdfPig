﻿namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    using Core;

    /// <summary>
    /// Relative move to command. starts a new subpath of the current path in the same manner as moveto.
    /// However, the number pair is interpreted as a displacement relative to the current point (x, y) rather than as an absolute coordinate.
    /// </summary>
    /// <remarks>
    /// moveto: moveto sets the current point in the graphics state to the user space coordinate (x, y) without adding any line segments to the current path.
    /// If the previous path operation in the current path was also a moveto or rmoveto, 
    /// that point is deleted from the current path and the new moveto point replaces it.
    /// </remarks>
    internal static class RMoveToCommand
    {
        public const string Name = "rmoveto";

        public static readonly byte First = 21;
        public static readonly byte? Second = null;

        public static bool TakeFromStackBottom { get; } = true;
        public static bool ClearsOperandStack { get; } = true;

        public static LazyType1Command Lazy { get; } = new LazyType1Command(Name, Run);

        public static void Run(Type1BuildCharContext context)
        {
            var deltaX = context.Stack.PopBottom();
            var deltaY = context.Stack.PopBottom();

            if (context.IsFlexing)
            {
                context.AddFlexPoint(new PdfPoint(deltaX, deltaY));
            }
            else
            {
                var x = context.CurrentPosition.X + deltaX;
                var y = context.CurrentPosition.Y + deltaY;
                context.CurrentPosition = new PdfPoint(x, y);

                context.Path.Add(new PdfSubpath());
                context.Path[context.Path.Count - 1].MoveTo(x, y);
            }

            context.Stack.Clear();
        }
    }
}
