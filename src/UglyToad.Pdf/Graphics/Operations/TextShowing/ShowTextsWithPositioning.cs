namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    using System;
    using Content;

    internal class ShowTextsWithPositioning : IGraphicsStateOperation
    {
        public const string Symbol = "TJ";

        public string Operator => Symbol;

        public object[] Array { get; }

        public ShowTextsWithPositioning(object[] array)
        {
            Array = array;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            throw new NotImplementedException();
        }
    }
}