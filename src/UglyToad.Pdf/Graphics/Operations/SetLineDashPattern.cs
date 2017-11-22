namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetLineDashPattern : IGraphicsStateOperation
    {
        public const string Symbol = "d";

        public string Operator => Symbol;

        public decimal[] Array { get; }

        public decimal Phase { get; }

        public SetLineDashPattern(decimal[] array, decimal phase)
        {
            Array = array;
            Phase = phase;
        }

        public override string ToString()
        {
            return $"{Array} {Phase} {Symbol}";
        }
    }
}