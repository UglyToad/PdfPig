namespace UglyToad.PdfPig.Graphics.Operations.TextObjects
{
    using System.IO;
    using Content;
    using PdfPig.Core;

    /// <summary>
    /// Begin a text object, initializing the text matrix and the text line matrix to the identity matrix. Text objects cannot be nested.
    /// </summary>
    internal class BeginText : IGraphicsStateOperation
    {
        public const string Symbol = "BT";
        public static readonly BeginText Value = new BeginText();

        public string Operator => Symbol;

        private BeginText()
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