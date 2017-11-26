namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    internal class SetTextRise : IGraphicsStateOperation
    {
        public const string Symbol = "Ts";

        public string Operator => Symbol;

        public decimal Rise { get; }

        public SetTextRise(decimal rise)
        {
            Rise = rise;
        }

        public override string ToString()
        {
            return $"{Rise} {Symbol}";
        }
    }
}