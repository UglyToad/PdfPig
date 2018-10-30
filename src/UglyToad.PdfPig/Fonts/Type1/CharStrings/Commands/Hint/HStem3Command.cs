namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the vertical ranges of three horizontal stem zones:
    /// 1st: between y0 and y0 + dy0
    /// 2nd: between y1 and y1 + dy1
    /// 3rd: between y2 and y2 + dy2
    /// Where y0, y1 and y2 are all relative to the y coordinate of the left sidebearing point.
    /// </summary>
    /// <remarks>
    /// Suited to letters with 3 horizontal stems like 'E'.
    /// </remarks>
    internal class HStem3Command
    {
        public const string Name = "hstem3";

        public static readonly byte First = 12;
        public static readonly byte? Second = 2;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int Y0 { get; }

        public int DeltaY0 { get; }

        public int Y1 { get; }

        public int DeltaY1 { get; }

        public int Y2 { get; }

        public int DeltaY2 { get; }

        public HStem3Command(int y0, int deltaY0, int y1, int deltaY1, int y2, int deltaY2)
        {
            Y0 = y0;
            DeltaY0 = deltaY0;
            Y1 = y1;
            DeltaY1 = deltaY1;
            Y2 = y2;
            DeltaY2 = deltaY2;
        }

        public override string ToString()
        {
            return $"{Y0} {DeltaY0} {Y1} {DeltaY1} {Y2} {DeltaY2} {Name}";
        }
    }
}
