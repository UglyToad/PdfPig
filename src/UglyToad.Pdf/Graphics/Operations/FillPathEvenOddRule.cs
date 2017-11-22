namespace UglyToad.Pdf.Graphics.Operations
{
    internal class FillPathEvenOddRule : IGraphicsStateOperation
    {
        public const string Symbol = "f*";

        public static readonly FillPathEvenOddRule Value = new FillPathEvenOddRule();

        public string Operator => Symbol;

        private FillPathEvenOddRule()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}