namespace UglyToad.Pdf.Graphics.Operations
{
    internal class SetStrokeColorDeviceGray : IGraphicsStateOperation
    {
        public const string Symbol = "G";

        public string Operator => Symbol;

        public decimal Gray { get; }

        public SetStrokeColorDeviceGray(decimal gray)
        {
            Gray = gray;
        }

        public override string ToString()
        {
            return $"{Gray} {Symbol}";
        }
    }
}