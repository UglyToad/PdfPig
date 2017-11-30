namespace UglyToad.Pdf.Graphics.Operations.TextPositioning
{
    using Content;
    using TextState;

    internal class MoveToNextLineWithOffsetSetLeading : IGraphicsStateOperation
    {
        public const string Symbol = "TD";

        public string Operator => Symbol;

        public decimal Tx { get; }

        public decimal Ty { get; }

        public MoveToNextLineWithOffsetSetLeading(decimal tx, decimal ty)
        {
            Tx = tx;
            Ty = ty;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var tlOperation = new SetTextLeading(-Ty);

            tlOperation.Run(operationContext, resourceStore);

            var tdOperation = new MoveToNextLineWithOffset(Tx, Ty);

            tdOperation.Run(operationContext, resourceStore);
        }

        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}