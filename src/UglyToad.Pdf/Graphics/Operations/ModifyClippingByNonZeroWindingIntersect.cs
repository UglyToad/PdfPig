namespace UglyToad.Pdf.Graphics.Operations
{
    internal class ModifyClippingByNonZeroWindingIntersect : IGraphicsStateOperation
    {
        public const string Symbol = "W";

        public static readonly ModifyClippingByNonZeroWindingIntersect Value = new ModifyClippingByNonZeroWindingIntersect();

        public string Operator => Symbol;

        private ModifyClippingByNonZeroWindingIntersect()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}