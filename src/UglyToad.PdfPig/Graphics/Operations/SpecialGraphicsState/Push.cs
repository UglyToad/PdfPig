namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System.IO;
    using Content;

    /// <summary>
    /// Save the current graphics state on the graphics state stack.
    /// </summary>
    internal class Push : IGraphicsStateOperation
    {
        public const string Symbol = "q";
        public static readonly Push Value = new Push();

        public string Operator => Symbol;

        private Push()
        {
        }

        public void Run(IOperationContext context, IResourceStore resourceStore)
        {
            context.PushState();
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
