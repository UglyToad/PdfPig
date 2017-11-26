namespace UglyToad.Pdf.Graphics.Operations.TextPositioning
{
    internal class MoveToNextLineWithOffset : IGraphicsStateOperation
    {
        public const string Symbol = "Td";

        public string Operator => Symbol;

        public decimal Tx { get; }

        public decimal Ty { get; }

        public MoveToNextLineWithOffset(decimal tx, decimal ty)
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