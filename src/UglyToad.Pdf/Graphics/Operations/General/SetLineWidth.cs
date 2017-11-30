namespace UglyToad.Pdf.Graphics.Operations.General
{
    using Content;

    internal class SetLineWidth : IGraphicsStateOperation
    {
        public const string Symbol = "w";

        public string Operator => Symbol;

        public decimal Width { get; }

        public SetLineWidth(decimal width)
        {
            Width = width;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.LineWidth = Width;
        }

        public override string ToString()
        {
            return $"{Width} {Symbol}";
        }
    }
}