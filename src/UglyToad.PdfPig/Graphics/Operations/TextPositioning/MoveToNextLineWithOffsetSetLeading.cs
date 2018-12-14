namespace UglyToad.PdfPig.Graphics.Operations.TextPositioning
{
    using System.IO;
    using Content;
    using TextState;

    /// <summary>
    /// Move to the start of the next line, offset from the start of the current line by (tx, ty).
    /// This operator also sets the leading parameter in the text state.
    /// </summary>
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

        public void Write(Stream stream)
        {
            stream.WriteDecimal(Tx);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Ty);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{Tx} {Ty} {Symbol}";
        }
    }
}