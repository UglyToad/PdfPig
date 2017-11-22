namespace UglyToad.Pdf.Graphics.Operations
{
    internal class CloseFillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "b*";

        public static readonly CloseFillPathEvenOddRuleAndStroke Value = new CloseFillPathEvenOddRuleAndStroke();

        public string Operator => Symbol;

        private CloseFillPathEvenOddRuleAndStroke()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}