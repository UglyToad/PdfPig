namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    internal class SetTextRenderingMode : IGraphicsStateOperation
    {
        public const string Symbol = "Tr";

        public string Operator => Symbol;

        public RenderingMode Mode { get; }

        public SetTextRenderingMode(int mode)
        {
            Mode = (RenderingMode)mode;
        }

        public override string ToString()
        {
            return $"{Mode} {Symbol}";
        }
    }
}