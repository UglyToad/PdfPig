namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetNonStrokeColorDeviceRgb : IGraphicsStateOperation
    {
        public const string Symbol = "rg";

        public string Operator => Symbol;

        public decimal R { get; }

        public decimal G { get; }

        public decimal B { get; }

        public SetNonStrokeColorDeviceRgb(decimal r, decimal g, decimal b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override string ToString()
        {
            return $"{R} {G} {B} {Symbol}";
        }
    }
}