namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetLineWidth : IGraphicsStateOperation
    {
        public const string Symbol = "w";

        public string Operator => Symbol;

        public decimal Width { get; }

        public SetLineWidth(decimal width)
        {
            Width = width;
        }

        public override string ToString()
        {
            return $"{Width} {Symbol}";
        }
    }
}