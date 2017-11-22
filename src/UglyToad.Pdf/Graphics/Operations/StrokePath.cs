namespace UglyToad.Pdf.Graphics.Operations
{
    internal class StrokePath : IGraphicsStateOperation
    {
        public const string Symbol = "S";

        public static readonly StrokePath Value = new StrokePath();

        public string Operator => Symbol;

        private StrokePath()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}