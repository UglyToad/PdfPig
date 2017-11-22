namespace UglyToad.Pdf.Graphics.Operations
{
    internal class FillPathNonZeroWindingAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "B";

        public static readonly FillPathNonZeroWindingAndStroke Value = new FillPathNonZeroWindingAndStroke();

        public string Operator => Symbol;

        private FillPathNonZeroWindingAndStroke()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}