namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;
    using Tokens;

    internal class InvokeNamedXObject : IGraphicsStateOperation
    {
        public const string Symbol = "Do";

        public string Operator => Symbol;

        public NameToken Name { get; }

        public InvokeNamedXObject(NameToken name)
        {
            Name = name;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var xobject = resourceStore.GetXObject(Name);

            operationContext.ApplyXObject(xobject);
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Name} {Symbol}";
        }
    }
}