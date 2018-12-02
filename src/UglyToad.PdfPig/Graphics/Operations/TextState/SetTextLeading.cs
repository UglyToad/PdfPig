namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
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

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Leading} {Symbol}";
        }
    }
}