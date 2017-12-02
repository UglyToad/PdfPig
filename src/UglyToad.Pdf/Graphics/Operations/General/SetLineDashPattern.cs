namespace UglyToad.Pdf.Graphics.Operations.General
{
    using Content;
    using Core;

    internal class SetLineDashPattern : IGraphicsStateOperation
    {
        public const string Symbol = "d";

        public string Operator => Symbol;

        public LineDashPattern Pattern { get; }
        
        public SetLineDashPattern(decimal[] array, int phase)
        {
            Pattern = new LineDashPattern(phase, array);
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.GetCurrentState().LineDashPattern = Pattern;
        }

        public override string ToString()
        {
            return $"{Pattern.Array} {Pattern.Phase} {Symbol}";
        }
    }
}