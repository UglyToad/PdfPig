namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;
    using Content;

    /// <summary>
    /// Move to the start of the next line.
    /// </summary>
    /// <remarks>
    /// This performs this operation: 0 -Tl Td
    /// The offset is negative leading text (Tl) value, this is incorrect in the specification.
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
            var tdOperation = new MoveToNextLineWithOffset(0, -1 * operationContext.GetCurrentState().FontState.Leading);

            tdOperation.Run(operationContext, resourceStore);
        }

        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}