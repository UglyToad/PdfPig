namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    internal class SetTextLeading : IGraphicsStateOperation
    {
        public const string Symbol = "TL";

        public string Operator => Symbol;

        public decimal Leading { get; }

        public SetTextLeading(decimal leading)
        {
            Leading = leading;
        }

        public override string ToString()
        {
            return $"{Leading} {Symbol}";
        }
    }
}