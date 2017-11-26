namespace UglyToad.Pdf.Graphics.Operations.PathConstruction
{
    internal class CloseSubpath : IGraphicsStateOperation
    {
        public const string Symbol = "h";

        public static readonly CloseSubpath Value = new CloseSubpath();

        public string Operator => Symbol;

        private CloseSubpath()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}