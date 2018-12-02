namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
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

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Gray} {Symbol}";
        }
    }
}