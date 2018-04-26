namespace UglyToad.PdfPig.Graphics.Operations
{
    using Content;
    using Tokenization.Tokens;

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

        public override string ToString()
        {
            return $"{Name} {Symbol}";
        }
    }
}