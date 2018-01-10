namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
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

        public override string ToString()
        {
            return $"{Rise} {Symbol}";
        }
    }
}