namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;

    internal class SetTextRise : IGraphicsStateOperation
    {
        public const string Symbol = "Ts";

        public string Operator => Symbol;

        public decimal Rise { get; }

        public SetTextRise(decimal rise)
        {
            Rise = rise;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.Rise = Rise;
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Rise} {Symbol}";
        }
    }
}