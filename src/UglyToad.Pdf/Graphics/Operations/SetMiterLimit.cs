namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetMiterLimit : IGraphicsStateOperation
    {
        public const string Symbol = "M";

        public string Operator => Symbol;

        public decimal Limit { get; }

        public SetMiterLimit(decimal limit)
        {
            Limit = limit;
        }

        public override string ToString()
        {
            return $"{Limit} {Symbol}";
        }
    }
}