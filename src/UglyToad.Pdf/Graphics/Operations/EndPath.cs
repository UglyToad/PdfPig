namespace UglyToad.Pdf.Graphics.Operations
{
    internal class EndPath : IGraphicsStateOperation
    {
        public const string Symbol = "n";

        public static readonly EndPath Value = new EndPath();

        public string Operator => Symbol;

        private EndPath()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}