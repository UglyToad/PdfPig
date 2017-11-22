namespace UglyToad.Pdf.Graphics.Operations
{
    internal class FillPathNonZeroWindingCompatibility : IGraphicsStateOperation
    {
        public const string Symbol = "F";

        public static readonly FillPathNonZeroWindingCompatibility Value = new FillPathNonZeroWindingCompatibility();

        public string Operator => Symbol;

        private FillPathNonZeroWindingCompatibility()
        {
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}