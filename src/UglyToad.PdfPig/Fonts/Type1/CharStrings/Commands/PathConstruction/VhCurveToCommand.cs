namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Vertical-horizontal curveto. 
    /// Equivalent to 0 dy1 dx2 dy2 dx3 0 rrcurveto.
    /// This command eliminates two arguments from an rrcurveto call when the first Bézier tangent is vertical and the second Bézier tangent is horizontal.
    /// </summary>
    internal class VhCurveToCommand
    {
        public const string Name = "vhcurveto";

        public static readonly byte First = 30;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int PostControlPointDeltaY { get; }

        public int PreControlPointDeltaX { get; }

        public int PreControlPointDeltaY { get; }

        public int EndPointDeltaX { get; }

        public VhCurveToCommand(int postControlPointDeltaY, int preControlPointDeltaX, int preControlPointDeltaY, int endPointDeltaX)
        {
            PostControlPointDeltaY = postControlPointDeltaY;
            PreControlPointDeltaX = preControlPointDeltaX;
            PreControlPointDeltaY = preControlPointDeltaY;
            EndPointDeltaX = endPointDeltaX;
        }
    }
}
