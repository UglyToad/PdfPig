namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System.Collections.Generic;
    using Core;

    /// <summary>
    /// The context used and updated when interpreting the commands for a charstring.
    /// </summary>
    internal class Type2BuildCharContext
    {
        private readonly Dictionary<int, double> transientArray = new Dictionary<int, double>();

        /// <summary>
        /// The numbers currently on the Type 2 Build Char stack.
        /// </summary>
        public CharStringStack Stack { get; } = new CharStringStack();

        /// <summary>
        /// The current path.
        /// </summary>
        public PdfPath Path { get; } = new PdfPath();

        /// <summary>
        /// The current location of the active point.
        /// </summary>
        public PdfPoint CurrentLocation { get; set; } = new PdfPoint(0, 0);

        /// <summary>
        ///  If the charstring has a width other than that of defaultWidthX it must be specified as the first
        ///  number in the charstring, and encoded as the difference from nominalWidthX.
        /// </summary>
        public double? Width { get; set; }

        public void AddRelativeHorizontalLine(double dx)
        {
            AddRelativeLine(dx, 0);
        }

        public void AddRelativeVerticalLine(double dy)
        {
            AddRelativeLine(0, dy);
        }

        public void AddRelativeBezierCurve(double dx1, double dy1, double dx2, double dy2, double dx3, double dy3)
        {
            var x1 = CurrentLocation.X + dx1;
            var y1 = CurrentLocation.Y + dy1;

            var x2 = x1 + dx2;
            var y2 = y1 + dy2;

            var x3 = x2 + dx3;
            var y3 = y2 + dy3;

            Path.BezierCurveTo(x1, y1, x2, y2, x3, y3);
            CurrentLocation = new PdfPoint(x3, y3);
        }

        public void AddRelativeLine(double dx, double dy)
        {
            var dest = new PdfPoint(CurrentLocation.X + dx, CurrentLocation.Y + dy);

            Path.LineTo(dest.X, dest.Y);
            CurrentLocation = dest;
        }

        public void AddVerticalStemHints(IReadOnlyList<(double start, double end)> hints)
        {
        }

        public void AddHorizontalStemHints(IReadOnlyList<(double start, double end)> hints)
        {
        }

        public void AddToTransientArray(double value, int location)
        {
            transientArray[location] = value;
        }

        public double GetFromTransientArray(int location)
        {
            var result = transientArray[location];
            transientArray.Remove(location);
            return result;
        }

        public static int CountToBias(int count)
        {
            if (count < 1240)
            {
                return 107;
            }

            if (count < 33900)
            {
                return 1131;
            }

            return 32768;
        }
    }
}