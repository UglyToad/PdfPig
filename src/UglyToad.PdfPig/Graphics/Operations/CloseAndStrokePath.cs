namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class CloseAndStrokePath : IGraphicsStateOperation
    {
        public const string Symbol = "s";

        public static readonly CloseAndStrokePath Value = new CloseAndStrokePath();

        public string Operator => Symbol;

        private CloseAndStrokePath()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.StrokePath(true);
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