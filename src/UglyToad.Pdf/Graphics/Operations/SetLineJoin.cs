namespace UglyToad.Pdf.Graphics.Operations
{
    using System;

    internal class SetLineJoin : IGraphicsStateOperation
    {
        public const string Symbol = "j";

        public string Operator => Symbol;

        public Style Join { get; set; }

        public SetLineJoin(int join) : this((Style)join) { }
        public SetLineJoin(Style join)
        {
            if (join < 0 || (int)join > 2)
            {
                throw new ArgumentException("Invalid argument passed for line join style. Should be 0, 1 or 2; instead got: " + join);
            }

            Join = join;
        }

        public override string ToString()
        {
            return $"{(int)Join} {Symbol}";
        }

        public enum Style
        {
            Miter = 0,
            Round = 1,
            Bevel = 2
        }
    }
}