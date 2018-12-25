namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System;
    using System.IO;
    using Content;
    using Core;

    internal class SetLineCap : IGraphicsStateOperation
    {
        public const string Symbol = "J";

        public string Operator => Symbol;

        public LineCapStyle Cap { get; }

        public SetLineCap(int cap) : this((LineCapStyle)cap) { }
        public SetLineCap(LineCapStyle cap)
        {
            if (cap < 0 || (int)cap > 2)
            {
                throw new ArgumentException("Invalid argument passed for line cap style. Should be 0, 1 or 2; instead got: " + cap);
            }

            Cap = cap;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.GetCurrentState().CapStyle = Cap;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal((int)Cap);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{(int) Cap} {Symbol}";
        }
    }
}