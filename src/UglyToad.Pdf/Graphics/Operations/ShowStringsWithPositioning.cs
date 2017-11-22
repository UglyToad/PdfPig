namespace UglyToad.Pdf.Graphics.Operations
{
    internal class ShowStringsWithPositioning : IGraphicsStateOperation
    {
        public const string Symbol = "TJ";

        public string Operator => Symbol;

        public object[] Array { get; }

        public ShowStringsWithPositioning(object[] array)
        {
            Array = array;
        }
    }
}