namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;

    internal class SetHorizontalScaling : IGraphicsStateOperation
    {
        public const string Symbol = "Tz";

        public string Operator => Symbol;

        public decimal Scale { get; }

        public SetHorizontalScaling(decimal scale)
        {
            Scale = scale;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.HorizontalScaling = Scale;
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Scale} {Symbol}";
        }
    }
}