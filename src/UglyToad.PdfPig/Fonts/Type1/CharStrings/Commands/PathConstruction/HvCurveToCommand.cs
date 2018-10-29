namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.PathConstruction
{
    /// <summary>
    /// Horizontal vertical curve to command. Draws a Bézier curve when the first Bézier tangent is horizontal and the second Bézier tangent is vertical.
    /// Equivalent to dx1 0 dx2 dy2 0 dy3 rrcurveto.
    /// </summary>
    internal class HvCurveToCommand
    {
        public const string Name = "hvcurveto";

        public static readonly byte First = 31;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int PostControlPointDeltaX { get; }

        public int PreControlPointDeltaX { get; }

        public int PreControlPointDeltaY { get; }

        public int EndPointDeltaY { get; }

        /// <summary>
        /// Create a new <see cref="HvCurveToCommand"/>.
        /// </summary>
        public HvCurveToCommand(int postControlPointDeltaX, int preControlPointDeltaX, int preControlPointDeltaY, int endPointDeltaY)
        {
            PostControlPointDeltaX = postControlPointDeltaX;
            PreControlPointDeltaX = preControlPointDeltaX;
            PreControlPointDeltaY = preControlPointDeltaY;
            EndPointDeltaY = endPointDeltaY;
        }

        public override string ToString()
        {
            return $"{PostControlPointDeltaX} {PreControlPointDeltaX} {PreControlPointDeltaY} {EndPointDeltaY} {Name}";
        }
    }
}
