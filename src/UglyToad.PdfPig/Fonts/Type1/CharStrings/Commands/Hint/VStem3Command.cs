namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.Hint
{
    /// <summary>
    /// Declares the horizontal ranges of three vertical stem zones.
    /// 1st: between x0 and x0 + dx0
    /// 2nd: between x1 and x1 + dx1
    /// 3rd: between x2 and x2 + dx2
    /// Where x0, x1 and x2 are all relative to the x coordinate of the left sidebearing point.
    /// </summary>
    /// <remarks>
    /// Suited to letters with 3 vertical stems, for instance 'm'.
    /// </remarks>
    internal class VStem3Command
    {
        public const string Name = "vstem3";

        public static readonly byte First = 12;
        public static readonly byte? Second = 1;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int X0 { get; }

        public int DeltaX0 { get; }

        public int X1 { get; }

        public int DeltaX1 { get; }

        public int X2 { get; }

        public int DeltaX2 { get; }

        public VStem3Command(int x0, int deltaX0, int x1, int deltaX1, int x2, int deltaX2)
        {
            X0 = x0;
            DeltaX0 = deltaX0;
            X1 = x1;
            DeltaX1 = deltaX1;
            X2 = x2;
            DeltaX2 = deltaX2;
        }

        public override string ToString()
        {
            return $"{X0} {DeltaX0} {X1} {DeltaX1} {X2} {DeltaX2} {Name}";
        }
    }
}
