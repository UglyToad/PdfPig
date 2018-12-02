namespace UglyToad.PdfPig.Graphics.Operations.TextObjects
{
    using System.IO;
    using Content;
    using PdfPig.Core;

    internal class EndText : IGraphicsStateOperation
    {
        public const string Symbol = "ET";
        public static readonly EndText Value = new EndText();

        public string Operator => Symbol;

        private EndText()
        {
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.TextMatrices.TextMatrix = TransformationMatrix.Identity;
            operationContext.TextMatrices.TextLineMatrix = TransformationMatrix.Identity;
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