namespace UglyToad.Pdf.Graphics.Operations.TextPositioning
{
    internal class MoveToNextLine : IGraphicsStateOperation
    {
        public const string Symbol = "T*";
        public static readonly MoveToNextLine Value = new MoveToNextLine();

        public string Operator => Symbol;

        private MoveToNextLine()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}