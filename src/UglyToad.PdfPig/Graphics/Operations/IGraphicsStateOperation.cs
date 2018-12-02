namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal interface IGraphicsStateOperation
    {
        string Operator { get; }

        void Run(IOperationContext operationContext, IResourceStore resourceStore);

        void Write(Stream stream);
    }
}