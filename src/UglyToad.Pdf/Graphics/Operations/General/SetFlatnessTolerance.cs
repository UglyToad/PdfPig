namespace UglyToad.Pdf.Graphics.Operations.General
{
    internal class SetFlatnessTolerance : IGraphicsStateOperation
    {
        public const string Symbol = "i";

        public string Operator => Symbol;

        public decimal Tolerance { get; }

        public SetFlatnessTolerance(decimal tolerance)
        {
            Tolerance = tolerance;
        }

        public override string ToString()
        {
            return $"{Tolerance} {Symbol}";
        }
    }
}