namespace UglyToad.Pdf.Graphics.Operations
{
    internal class BeginText : IGraphicsStateOperation
    {
        public const string Symbol = "BT";
        public static readonly BeginText Value = new BeginText();

        public string Operator => Symbol;

        private BeginText()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}