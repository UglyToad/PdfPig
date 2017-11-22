namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetTextRenderingMode : IGraphicsStateOperation
    {
        public const string Symbol = "Tr";

        public string Operator => Symbol;

        public int Mode { get; }

        public SetTextRenderingMode(int mode)
        {
            Mode = mode;
        }

        public override string ToString()
        {
            return $"{Mode} {Symbol}";
        }
    }
}