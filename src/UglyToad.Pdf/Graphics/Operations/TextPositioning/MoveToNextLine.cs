namespace UglyToad.Pdf.Graphics.Operations.TextPositioning
{
    using Content;

    /// <summary>
    /// Move to the start of the next line.
    /// </summary>
    /// <remarks>
    /// This performs this operation: 0 Tl Td
    /// </remarks>
    internal class MoveToNextLine : IGraphicsStateOperation
    {
        public const string Symbol = "T*";
        public static readonly MoveToNextLine Value = new MoveToNextLine();

        public string Operator => Symbol;

        private MoveToNextLine()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var tdOperation = new MoveToNextLineWithOffset(0, operationContext.GetCurrentState().FontState.Leading);

            tdOperation.Run(operationContext, resourceStore);
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}