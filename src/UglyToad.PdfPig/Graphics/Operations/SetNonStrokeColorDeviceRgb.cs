namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Content;

    internal class SetNonStrokeColorDeviceRgb : IGraphicsStateOperation
    {
        public const string Symbol = "rg";

        public string Operator => Symbol;

        public decimal R { get; }

        public decimal G { get; }

        public decimal B { get; }

        public SetNonStrokeColorDeviceRgb(decimal r, decimal g, decimal b)
        {
            R = r;
            G = g;
            B = b;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(R);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(G);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(B);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{R} {G} {B} {Symbol}";
        }
    }
}