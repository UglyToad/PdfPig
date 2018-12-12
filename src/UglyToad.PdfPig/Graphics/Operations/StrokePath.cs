namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class StrokePath : IGraphicsStateOperation
    {
        public const string Symbol = "S";

        public static readonly StrokePath Value = new StrokePath();

        public string Operator => Symbol;

        private StrokePath()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.StrokePath(false);
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