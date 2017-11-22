namespace UglyToad.Pdf.Graphics.Operations
{
    internal class MoveToNextLineWithOffsetSetLeading : IGraphicsStateOperation
    {
        public const string Symbol = "TD";

        public string Operator => Symbol;

        public decimal Tx { get; }

        public decimal Ty { get; }

        public MoveToNextLineWithOffsetSetLeading(decimal tx, decimal ty)
        {
            Tx = tx;
            Ty = ty;
        }

        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}