namespace UglyToad.Pdf.Graphics.Operations
{
    internal class Pop : IGraphicsStateOperation
    {
        public const string Symbol = "Q";
        public static readonly Pop Value = new Pop();

        public string Operator => Symbol;

        private Pop()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}