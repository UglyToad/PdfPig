namespace UglyToad.Pdf.Graphics.Operations.General
{
    using System;

    internal class SetLineCap : IGraphicsStateOperation
    {
        public const string Symbol = "J";

        public string Operator => Symbol;

        public LineCapStyle Cap { get; set; }

        public SetLineCap(int cap) : this((LineCapStyle)cap) { }
        public SetLineCap(LineCapStyle cap)
        {
            if (cap < 0 || (int)cap > 2)
            {
                throw new ArgumentException("Invalid argument passed for line cap style. Should be 0, 1 or 2; instead got: " + cap);
            }

            Cap = cap;
        }

        public override string ToString()
        {
            return $"{(int) Cap} {Symbol}";
        }
    }
}