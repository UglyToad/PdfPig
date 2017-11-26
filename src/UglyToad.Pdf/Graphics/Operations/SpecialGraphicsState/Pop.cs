namespace UglyToad.Pdf.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using Content;

    internal class Pop : IGraphicsStateOperation
    {
        public const string Symbol = "Q";
        public static readonly Pop Value = new Pop();

        public string Operator => Symbol;

        private Pop()
        {
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentStackSize = operationContext.StackSize;
            if (currentStackSize > 1)
            {
                operationContext.PopState();
            }
            else
            {
                throw new InvalidOperationException("Cannot execute a pop of the graphics state stack, it would leave the stack empty.");
            }
        }

        public override string ToString()
        {
            return Symbol;
        }
    }
}