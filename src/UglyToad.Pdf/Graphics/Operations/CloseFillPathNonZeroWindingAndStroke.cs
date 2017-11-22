namespace UglyToad.Pdf.Graphics.Operations
{
    internal class CloseFillPathNonZeroWindingAndStroke : IGraphicsStateOperation
    {
        public const string Symbol = "b";

        public static readonly CloseFillPathNonZeroWindingAndStroke Value = new CloseFillPathNonZeroWindingAndStroke();

        public string Operator => Symbol;

        private CloseFillPathNonZeroWindingAndStroke()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}