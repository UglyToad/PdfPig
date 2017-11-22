namespace UglyToad.Pdf.Graphics.Operations
{
    internal class FillPathNonZeroWinding : IGraphicsStateOperation
    {
        public const string Symbol = "f";

        public static readonly FillPathNonZeroWinding Value = new FillPathNonZeroWinding();

        public string Operator => Symbol;

        private FillPathNonZeroWinding()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}