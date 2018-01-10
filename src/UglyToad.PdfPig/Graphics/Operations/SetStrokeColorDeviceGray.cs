namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;

    internal class SetStrokeColorDeviceGray : IGraphicsStateOperation
    {
        public const string Symbol = "G";

        public string Operator => Symbol;

        public decimal Gray { get; }

        public SetStrokeColorDeviceGray(decimal gray)
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