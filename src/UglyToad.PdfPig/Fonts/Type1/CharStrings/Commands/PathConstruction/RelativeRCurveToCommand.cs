namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Relative rcurveto. Whereas the arguments to the rcurveto operator in the PostScript language are all relative to the current
    /// point, the arguments to rrcurveto are relative to each other. 
    /// Equivalent to: dx1 dy1 (dx1+dx2) (dy1+dy2) (dx1+dx2+dx3) (dy1+dy2+dy3) rcurveto.
    /// </summary>
    internal class RelativeRCurveToCommand
    {
        public const string Name = "rrcurveto";

        public static readonly byte First = 8;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int DeltaX1 { get; }

        public int DeltaY1 { get; }

        public int DeltaX2 { get; }

        public int DeltaY2 { get; }

        public int DeltaX3 { get; }

        public int DeltaY3 { get; }

        public RelativeRCurveToCommand(int deltaX1, int deltaY1, int deltaX2, int deltaY2, int deltaX3, int deltaY3)
        {
            DeltaX1 = deltaX1;
            DeltaY1 = deltaY1;
            DeltaX2 = deltaX2;
            DeltaY2 = deltaY2;
            DeltaX3 = deltaX3;
            DeltaY3 = deltaY3;
        }
    }
}
