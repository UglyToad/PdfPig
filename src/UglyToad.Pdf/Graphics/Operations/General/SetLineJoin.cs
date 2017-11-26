namespace UglyToad.Pdf.Graphics.Operations.General
{
    using System;

    internal class SetLineJoin : IGraphicsStateOperation
    {
        public const string Symbol = "j";

        public string Operator => Symbol;

        public LineJoinStyle Join { get; set; }

        public SetLineJoin(int join) : this((LineJoinStyle)join) { }
        public SetLineJoin(LineJoinStyle join)
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
    }
}