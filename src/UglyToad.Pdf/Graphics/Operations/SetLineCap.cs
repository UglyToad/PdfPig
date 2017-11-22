namespace UglyToad.Pdf.Graphics.Operations
{
    using System;

    internal class SetLineCap : IGraphicsStateOperation
    {
        public const string Symbol = "J";

        public string Operator => Symbol;

        public Style Cap { get; set; }

        public SetLineCap(int cap) : this((Style)cap) { }
        public SetLineCap(Style cap)
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

        public enum Style
        {
            Butt = 0,
            Round = 1,
            ProjectingSquare = 2
        }
    }
}