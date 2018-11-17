namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Geometry;

    /// <summary>
    /// Represents the deferred execution of a Type 2 CharString command.
    /// </summary>
    internal class LazyType2Command
    {
        private readonly Action<Type2BuildCharContext> runCommand;

        public string Name { get; }

        public LazyType2Command(string name, Action<Type2BuildCharContext> runCommand)
        {
            Name = name;
            this.runCommand = runCommand ?? throw new ArgumentNullException(nameof(runCommand));
        }

        [DebuggerStepThrough]
        public void Run(Type2BuildCharContext context)
        {
            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class Type2BuildCharContext
    {
        private readonly Dictionary<int, decimal> transientArray = new Dictionary<int, decimal>();

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharacterPath Path { get; } = new CharacterPath();

        public PdfPoint CurrentLocation { get; set; } = new PdfPoint(0, 0);

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
    }
}
