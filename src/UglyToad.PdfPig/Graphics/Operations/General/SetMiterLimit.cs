namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System.IO;
    using Content;

    internal class SetMiterLimit : IGraphicsStateOperation
    {
        public const string Symbol = "M";

        public string Operator => Symbol;

        public decimal Limit { get; }

        public SetMiterLimit(decimal limit)
        {
            Limit = limit;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.MiterLimit = Limit;
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Limit} {Symbol}";
        }
    }
}