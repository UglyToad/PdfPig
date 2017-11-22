namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetHorizontalScaling : IGraphicsStateOperation
    {
        public const string Symbol = "Tz";

        public string Operator => Symbol;

        public decimal Scale { get; }

        public SetHorizontalScaling(decimal scale)
        {
            Scale = scale;
        }

        public override string ToString()
        {
            return $"{Scale} {Symbol}";
        }
    }
}