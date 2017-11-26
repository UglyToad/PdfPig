namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    internal class SetCharacterSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "Tc";

        public string Operator => Symbol;

        public decimal Spacing { get; }

        public SetCharacterSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}