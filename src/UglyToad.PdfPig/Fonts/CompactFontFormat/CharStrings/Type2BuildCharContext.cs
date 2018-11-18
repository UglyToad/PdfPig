namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System.Collections.Generic;
    using Geometry;

    /// <summary>
    /// The context used and updated when interpreting the commands for a charstring.
    /// </summary>
    internal class Type2BuildCharContext
    {
        private readonly Dictionary<int, decimal> transientArray = new Dictionary<int, decimal>();

        /// <summary>
        /// The local subroutines available in this font.
        /// </summary>
        public IReadOnlyDictionary<int, Type2CharStrings.CommandSequence> LocalSubroutines { get; }

        /// <summary>
        /// The global subroutines available in this font set.
        /// </summary>
        public IReadOnlyDictionary<int, Type2CharStrings.CommandSequence> GlobalSubroutines { get; }

        /// <summary>
        /// The numbers currently on the Type 2 Build Char stack.
        /// </summary>
        public CharStringStack Stack { get; } = new CharStringStack();

        /// <summary>
        /// The current path.
        /// </summary>
        public CharacterPath Path { get; } = new CharacterPath();

        /// <summary>
        /// The current location of the active point.
        /// </summary>
        public PdfPoint CurrentLocation { get; set; } = new PdfPoint(0, 0);

        /// <summary>
        ///  If the charstring has a width other than that of defaultWidthX it must be specified as the first
        ///  number in the charstring, and encoded as the difference from nominalWidthX.
        /// </summary>
        public decimal? Width { get; set; }

        /// <summary>
        /// Create a new <see cref="Type2BuildCharContext"/>.
        /// </summary>
        public Type2BuildCharContext(IReadOnlyDictionary<int, Type2CharStrings.CommandSequence> localSubroutines, 
            IReadOnlyDictionary<int, Type2CharStrings.CommandSequence> globalSubroutines)
        {
            LocalSubroutines = localSubroutines;
            GlobalSubroutines = globalSubroutines;
        }
        
        public void AddRelativeHorizontalLine(decimal dx)
        {
            AddRelativeLine(dx, 0);
        }

        public void AddRelativeVerticalLine(decimal dy)
        {
            AddRelativeLine(0, dy);
        }

        public void AddRelativeBezierCurve(decimal dx1, decimal dy1, decimal dx2, decimal dy2, decimal dx3, decimal dy3)
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

        public void AddRelativeLine(decimal dx, decimal dy)
        {
            var dest = new PdfPoint(CurrentLocation.X + dx, CurrentLocation.Y + dy);

            Path.LineTo(dest.X, dest.Y);
            CurrentLocation = dest;
        }

        public void AddVerticalStemHints(IReadOnlyList<(decimal start, decimal end)> hints)
        {
        }

        public void AddHorizontalStemHints(IReadOnlyList<(decimal start, decimal end)> hints)
        {
        }

        public void AddToTransientArray(decimal value, int location)
        {
            transientArray[location] = value;
        }

        public decimal GetFromTransientArray(int location)
        {
            var result = transientArray[location];
            transientArray.Remove(location);
            return result;
        }

        public int GetLocalSubroutineBias()
        {
            var count = LocalSubroutines.Count;
            return CountToBias(count);
        }

        public int GetGlobalSubroutineBias()
        {
            var count = GlobalSubroutines.Count;
            return CountToBias(count);
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

        public void EvaluateSubroutine(Type2CharStrings.CommandSequence subroutine)
        {
            foreach (var command in subroutine.Commands)
            {
                command.Match(x => Stack.Push(x),
                    act => act.Run(this));
            }
        }
    }
}