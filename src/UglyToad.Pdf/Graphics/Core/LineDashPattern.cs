namespace UglyToad.Pdf.Graphics.Core
{
    using System;

    internal struct LineDashPattern
    {
        public int Phase { get; }

        public decimal[] Array { get; }

        public LineDashPattern(int phase, decimal[] array)
        {
            Phase = phase;
            Array = array ?? throw new ArgumentNullException(nameof(array));
        }

        public static LineDashPattern Solid { get; }
            = new LineDashPattern(0, new decimal[0]);
    }
}
