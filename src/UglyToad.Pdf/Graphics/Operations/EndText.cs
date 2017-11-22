namespace UglyToad.Pdf.Graphics.Operations
{
    internal class EndText : IGraphicsStateOperation
    {
        public const string Symbol = "q";
        public static readonly EndText Value = new EndText();

        public string Operator => Symbol;

        private EndText()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}