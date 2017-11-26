namespace UglyToad.Pdf.Graphics.Operations.General
{
    internal class SetLineDashPattern : IGraphicsStateOperation
    {
        public const string Symbol = "d";

        public string Operator => Symbol;

        public LineDashPattern Pattern { get; }
        

        public SetLineDashPattern(decimal[] array, int phase)
        {
            Pattern = new LineDashPattern(phase, array);
        }

        public override string ToString()
        {
            return $"{Pattern.Array} {Pattern.Phase} {Symbol}";
        }
    }
}