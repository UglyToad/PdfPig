namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;
    using Core;

    internal class SetTextRenderingMode : IGraphicsStateOperation
    {
        public const string Symbol = "Tr";

        public string Operator => Symbol;

        public RenderingMode Mode { get; }

        public SetTextRenderingMode(int mode)
        {
            Mode = (RenderingMode)mode;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.RenderingMode = Mode;
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Mode} {Symbol}";
        }
    }
}