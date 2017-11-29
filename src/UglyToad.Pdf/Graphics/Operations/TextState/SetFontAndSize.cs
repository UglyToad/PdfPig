namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    using Content;
    using Cos;

    internal class SetFontAndSize : IGraphicsStateOperation
    {
        public const string Symbol = "Tf";

        public string Operator => Symbol;

        public CosName Font { get; }

        public decimal Size { get; }

        public SetFontAndSize(CosName font, decimal size)
        {
            Font = font;
            Size = size;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.FontSize = Size;
            currentState.FontState.FontName = Font;
        }

        public override string ToString()
        {
            return $"{Font} {Size} {Symbol}";
        }
    }
}