namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    internal class ShowTextsWithPositioning : IGraphicsStateOperation
    {
        public const string Symbol = "TJ";

        public string Operator => Symbol;

        public object[] Array { get; }

        public ShowTextsWithPositioning(object[] array)
        {
            Array = array;
        }
    }
}