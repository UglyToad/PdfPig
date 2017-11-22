namespace UglyToad.Pdf.Graphics.Operations
{
    internal class FillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "B*";

        public static readonly FillPathEvenOddRuleAndStroke Value = new FillPathEvenOddRuleAndStroke();

        public string Operator => Symbol;

        private FillPathEvenOddRuleAndStroke()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}