namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    using Content;

    internal class SetWordSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "Tw";

        public string Operator => Symbol;

        public decimal Spacing { get; }

        public SetWordSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.WordSpacing = Spacing;
        }

        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}