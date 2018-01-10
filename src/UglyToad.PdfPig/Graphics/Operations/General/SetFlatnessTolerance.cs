namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using Content;

    internal class SetFlatnessTolerance : IGraphicsStateOperation
    {
        public const string Symbol = "i";

        public string Operator => Symbol;

        public decimal Tolerance { get; }

        public SetFlatnessTolerance(decimal tolerance)
        {
            Tolerance = tolerance;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.GetCurrentState().Flatness = Tolerance;
        }

        public override string ToString()
        {
            return $"{Tolerance} {Symbol}";
        }
    }
}