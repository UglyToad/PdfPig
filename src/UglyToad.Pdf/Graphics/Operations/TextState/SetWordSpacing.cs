namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    internal class SetWordSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "Tw";

        public string Operator => Symbol;

        public decimal Spacing { get; }

        public SetWordSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}