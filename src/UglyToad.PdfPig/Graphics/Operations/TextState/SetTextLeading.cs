namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using Content;

    internal class SetTextLeading : IGraphicsStateOperation
    {
        public const string Symbol = "TL";

        public string Operator => Symbol;

        public decimal Leading { get; }

        public SetTextLeading(decimal leading)
        {
            Leading = leading;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.Leading = Leading;
        }

        public override string ToString()
        {
            return $"{Leading} {Symbol}";
        }
    }
}