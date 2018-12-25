namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System;
    using System.IO;
    using Content;
    using Core;

    internal class SetLineJoin : IGraphicsStateOperation
    {
        public const string Symbol = "j";

        public string Operator => Symbol;

        public LineJoinStyle Join { get; }

        public SetLineJoin(int join) : this((LineJoinStyle)join) { }
        public SetLineJoin(LineJoinStyle join)
        {
            if (join < 0 || (int)join > 2)
            {
                throw new ArgumentException("Invalid argument passed for line join style. Should be 0, 1 or 2; instead got: " + join);
            }

            Join = join;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.GetCurrentState().JoinStyle = Join;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal((int)Join);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{(int)Join} {Symbol}";
        }
    }
}