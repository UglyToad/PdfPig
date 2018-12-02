namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System.IO;
    using Content;

    internal class SetColorRenderingIntent : IGraphicsStateOperation
    {
        public const string Symbol = "ri";

        public string Operator => Symbol;

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {

        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }
    }
}