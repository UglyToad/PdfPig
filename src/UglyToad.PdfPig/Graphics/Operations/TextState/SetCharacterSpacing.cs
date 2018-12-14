namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System.IO;
    using Content;

    /// <summary>
    /// Set the character spacing to a number expressed in unscaled text space units.
    /// Initial value: 0. 
    /// </summary>
    internal class SetCharacterSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "Tc";

        public string Operator => Symbol;

        public decimal Spacing { get; }

        public SetCharacterSpacing(decimal spacing)
        {
            Spacing = spacing;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.CharacterSpacing = Spacing;
        }

        public void Write(Stream stream)
        {
            stream.WriteDecimal(Spacing);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        public override string ToString()
        {
            return $"{Spacing} {Symbol}";
        }
    }
}