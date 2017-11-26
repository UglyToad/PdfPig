namespace UglyToad.Pdf.Graphics.Operations
{
    internal class ModifyClippingByEvenOddIntersect : IGraphicsStateOperation
    {
        public const string Symbol = "W*";

        public static readonly ModifyClippingByEvenOddIntersect Value = new ModifyClippingByEvenOddIntersect();

        public string Operator => Symbol;

        private ModifyClippingByEvenOddIntersect()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}