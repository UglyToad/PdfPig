namespace UglyToad.Pdf.Graphics.Operations
{
    using Content;

    internal class SetNonStrokeColorDeviceGray : IGraphicsStateOperation
    {
        public const string Symbol = "g";

        public string Operator => Symbol;

        public decimal Gray { get; }

        public SetNonStrokeColorDeviceGray(decimal gray)
        {
            Gray = gray;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
        }

        public override string ToString()
        {
            return $"{Gray} {Symbol}";
        }
    }
}