namespace UglyToad.Pdf.Graphics.Operations
{
    internal class CloseAndStrokePath : IGraphicsStateOperation
    {
        public const string Symbol = "s";

        public static readonly CloseAndStrokePath Value = new CloseAndStrokePath();

        public string Operator => Symbol;

        private CloseAndStrokePath()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}