namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetNonStrokeColorDeviceGray : IGraphicsStateOperation
    {
        public const string Symbol = "g";

        public string Operator => Symbol;

        public decimal Gray { get; }

        public SetNonStrokeColorDeviceGray(decimal gray)
        {
            Gray = gray;
        }

        public override string ToString()
        {
            return $"{Gray} {Symbol}";
        }
    }
}