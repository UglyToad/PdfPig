namespace UglyToad.Pdf.Graphics.Operations
{
    using Cos;

    internal class SetFontSize : IGraphicsStateOperation
    {
        public const string Symbol = "Tf";

        public string Operator => Symbol;

        public CosName Font { get; }

        public decimal Size { get; }

        public SetFontSize(CosName font, decimal size)
        {
            Font = font;
            Size = size;
        }

        public override string ToString()
        {
            return $"{Font} {Size} {Symbol}";
        }
    }
}